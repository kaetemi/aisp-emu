using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaEventAccessNpcHandler(
    INpcRepository npcRepository,
    IShopRepository shopRepository,
    ServerScriptDispatcher serverScriptDispatcher,
    AdventureShopCatalog adventureShopCatalog,
    ITextLocaliser localiser,
    ILogger<AreaEventAccessNpcHandler> logger,
    DramaCatalog dramaCatalog,
    IItemboxRepository? itembox = null
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventAccessNpcRequest;
    public PacketType ResponseType => PacketType.EventAccessNpcResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = EventAccessNpcRequest.FromBytes(payload.Span);
        // Touching any NPC ends an open capsule machine; the client has already dropped its overlay.
        // Box deposits never left the bag in the database, so dropping the box state loses nothing.
        session.ActiveGachaMachineId = null;
        session.GachaTicketBox = null;

        logger.LogInformation(
            "EventAccessNpcRequest from {CharacterId}: npc={NpcId}, pos=({X},{Y},{Z})",
            session.CharacterId,
            request.NpcId,
            request.AvatarX,
            request.AvatarY,
            request.AvatarZ
        );

        if (session.ActiveEventKey != null)
        {
            logger.LogWarning(
                "Rejecting EventAccessNpcRequest for character {CharacterId}: already in event {EventKey}",
                session.CharacterId,
                session.ActiveEventKey
            );
            await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
            return;
        }

        var npc = await npcRepository.GetActiveByMapAndObjectIdAsync(
            session.MapId,
            session.ChannelId,
            request.NpcId,
            ct
        );
        if (npc is null || !npc.IsEnabled)
        {
            session.ActiveShopId = null;
            logger.LogWarning(
                "Rejecting EventAccessNpcRequest for character {CharacterId}: map={MapId}, requestedNpc={NpcId}",
                session.CharacterId,
                session.MapId,
                request.NpcId
            );
            await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
            return;
        }

        if (npc.EventKind != NpcEventKind.None && !string.IsNullOrWhiteSpace(npc.EventKey))
        {
            session.ActiveShopId = null;
            switch (npc.EventKind)
            {
                case NpcEventKind.ClientScript:
                    await session.SendAsync(
                        ResponseType,
                        new EventAccessNpcResponse(0).ToBytes(),
                        ct
                    );
                    logger.LogInformation(
                        "Starting client script {EventKey} for character {CharacterId} via npc {NpcId}",
                        npc.EventKey,
                        session.CharacterId,
                        request.NpcId
                    );
                    await ClientScriptLauncher.StartAsync(
                        session,
                        npc.EventKey,
                        EventCompletionPolicy.Replayable,
                        ct
                    );
                    return;
                case NpcEventKind.ServerScript:
                    if (!serverScriptDispatcher.HasScript(npc.EventKey))
                    {
                        logger.LogWarning(
                            "Rejecting EventAccessNpcRequest for character {CharacterId}: unknown server script {EventKey}",
                            session.CharacterId,
                            npc.EventKey
                        );
                        await session.SendAsync(
                            ResponseType,
                            new EventAccessNpcResponse(1).ToBytes(),
                            ct
                        );
                        return;
                    }

                    var context = new ServerScriptContext { Npc = npc };
                    if (
                        !await serverScriptDispatcher.CanStartAsync(
                            session,
                            npc.EventKey,
                            context,
                            ct
                        )
                    )
                    {
                        logger.LogInformation(
                            "Rejecting EventAccessNpcRequest for character {CharacterId}: server script {EventKey} is not currently available",
                            session.CharacterId,
                            npc.EventKey
                        );
                        await session.SendAsync(
                            ResponseType,
                            new EventAccessNpcResponse(1).ToBytes(),
                            ct
                        );
                        return;
                    }

                    await session.SendAsync(
                        ResponseType,
                        new EventAccessNpcResponse(0).ToBytes(),
                        ct
                    );
                    logger.LogInformation(
                        "Starting server script {EventKey} for character {CharacterId} via npc {NpcId}",
                        npc.EventKey,
                        session.CharacterId,
                        request.NpcId
                    );
                    await serverScriptDispatcher.StartAsync(
                        session,
                        npc.EventKey,
                        context,
                        serverScriptDispatcher.GetCompletionPolicy(npc.EventKey),
                        ct
                    );
                    return;
            }
        }

        if (
            npc.InteractionType
            is NpcInteractionType.AdventureShopBuy
                or NpcInteractionType.AdventureShopUpload
                or NpcInteractionType.NiconiCommonsShop
        )
        {
            session.ActiveShopId = null;
            if (
                npc.InteractionType == NpcInteractionType.NiconiCommonsShop
                && (npc.ShopId is null || npc.Shop is null || !npc.Shop.IsEnabled)
            )
            {
                await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
                return;
            }
            var adventureNpcObjectId = checked((uint)npc.NpcObjectId);
            await session.SendAsync(ResponseType, new EventAccessNpcResponse(0).ToBytes(), ct);
            await session.SendAsync(
                PacketType.NotifySupplyNpcExec,
                new NotifySupplyNpcExec(adventureNpcObjectId).ToBytes(),
                ct
            );

            switch (npc.InteractionType)
            {
                case NpcInteractionType.AdventureShopBuy:
                    // The client wants the catalog snapshot, not the NPC id.
                    var snapshot = await adventureShopCatalog.BuildSnapshotAsync(
                        session.User?.Id ?? session.UserId,
                        ct
                    );
                    await session.SendAsync(
                        PacketType.AdventureShopStartedNotify,
                        snapshot.ToBytes(),
                        ct
                    );
                    return;
                case NpcInteractionType.AdventureShopUpload:
                    await session.SendAsync(
                        PacketType.AdventureUploadStartedNotify,
                        new AdventureUploadStartedNotify(adventureNpcObjectId, 0).ToBytes(),
                        ct
                    );
                    return;
                case NpcInteractionType.NiconiCommonsShop:
                    session.ActiveShopId = npc.ShopId;
                    var shopName = localiser.Get(session, L.Shop.DisplayName(npc.Shop!.Code));
                    await session.SendAsync(
                        PacketType.NiconiCommonsShopStartedNotify,
                        new NiconiCommonsShopStartedNotify(
                            adventureNpcObjectId,
                            shopName
                        ).ToBytes(),
                        ct
                    );
                    await session.SendAsync(
                        PacketType.NiconiCommonsShopItemNotify,
                        (await dramaCatalog.SnapshotAsync(npc.ShopId!.Value, ct)).ToBytes(),
                        ct
                    );
                    return;
            }
        }

        if (npc.InteractionType == NpcInteractionType.Gacha)
        {
            session.ActiveShopId = null;
            var machine = npc.GachaMachine;
            if (npc.GachaMachineId is null || machine is null || !machine.IsEnabled)
            {
                logger.LogWarning(
                    "Rejecting EventAccessNpcRequest for character {CharacterId}: npc={NpcId} has no enabled gacha machine",
                    session.CharacterId,
                    request.NpcId
                );
                await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
                return;
            }

            session.ActiveGachaMachineId = machine.Id;
            var machineNpcObjectId = checked((uint)npc.NpcObjectId);
            await session.SendAsync(ResponseType, new EventAccessNpcResponse(0).ToBytes(), ct);
            await session.SendAsync(
                PacketType.NotifySupplyNpcExec,
                new NotifySupplyNpcExec(machineNpcObjectId).ToBytes(),
                ct
            );
            // The window draws the character's item box from the client's list as it stands when the started
            // notify builds it (entries that arrive later are stored but not drawn until the next open), so
            // the box goes first. The client keeps the list between windows and across machines, so it is
            // re-sent as delete + create pairs to land on exactly one copy of each entry.
            if (itembox is not null)
            {
                var boxed = await itembox.ListAsync(checked((int)session.CharacterId), ct);
                await Itembox.SendContentsAsync(session, boxed, ct);
            }
            await session.SendAsync(
                PacketType.GachaStartedNotify,
                new GachaStartedNotify(
                    localiser.Get(session, L.Gacha.DisplayName(machine.Code)),
                    checked((uint)machine.VisualId),
                    checked((ulong)machine.AiPrice),
                    checked((ulong)machine.NicoPrice),
                    checked((uint)(machine.TicketItemId ?? 0)),
                    machine.TicketItemId is null
                        ? (ushort)0
                        : checked((ushort)machine.TicketsPerPull)
                ).ToBytes(),
                ct
            );
            logger.LogInformation(
                "Opened gacha machine {Code} for character {CharacterId} via npc {NpcId}",
                machine.Code,
                session.CharacterId,
                request.NpcId
            );
            return;
        }

        if (
            npc.InteractionType != NpcInteractionType.Shop
            || npc.ShopId is null
            || npc.Shop is null
            || !npc.Shop.IsEnabled
        )
        {
            session.ActiveShopId = null;
            if (npc.InteractionType == NpcInteractionType.Decorative)
            {
                logger.LogDebug(
                    "Acknowledging decorative NPC access for character {CharacterId}: npc={NpcId}",
                    session.CharacterId,
                    request.NpcId
                );
                await session.SendAsync(ResponseType, new EventAccessNpcResponse(0).ToBytes(), ct);
                return;
            }

            logger.LogWarning(
                "Rejecting EventAccessNpcRequest for character {CharacterId}: map={MapId}, requestedNpc={NpcId}",
                session.CharacterId,
                session.MapId,
                request.NpcId
            );
            await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
            return;
        }

        var shopItems = await shopRepository.GetEnabledItemsAsync(npc.ShopId.Value, ct);
        if (shopItems.Count == 0)
        {
            session.ActiveShopId = null;
            logger.LogWarning(
                "Rejecting EventAccessNpcRequest for character {CharacterId}: npc={NpcId} has no enabled shop items",
                session.CharacterId,
                request.NpcId
            );
            await session.SendAsync(ResponseType, new EventAccessNpcResponse(1).ToBytes(), ct);
            return;
        }

        session.ActiveShopId = npc.ShopId.Value;
        var npcObjectId = checked((uint)npc.NpcObjectId);
        await session.SendAsync(ResponseType, new EventAccessNpcResponse(0).ToBytes(), ct);
        await session.SendAsync(
            PacketType.NotifySupplyNpcExec,
            new NotifySupplyNpcExec(npcObjectId).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.ShopStartedNotify,
            new ShopStartedNotify(
                npcObjectId,
                localiser.Get(session, L.Shop.DisplayName(npc.Shop.Code)),
                checked((uint)npc.Shop.BannerVisualId)
            ).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.ShopItemNotify,
            new ShopItemNotify(
                shopItems
                    .Select(x => new ShopItemEntry(
                        (uint)x.ItemId,
                        checked((ulong)x.AiPrice),
                        checked((ulong)x.NicoPrice)
                    ))
                    .ToList()
            ).ToBytes(),
            ct
        );
    }
}
