using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Test-machine crank. Debits the last <c>/gacha</c> price, grants
/// <see cref="GachaTestSession.PrizeItemId"/>, then
/// <c>recv_gacha_buy_r</c> + money + inventory so the capsule anim can run.
/// </summary>
public sealed class AreaGachaBuyHandler(
    IUserRepository userRepo,
    ICharacterRepository characterRepo,
    ILogger<AreaGachaBuyHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GachaBuyRequest;
    public PacketType ResponseType => PacketType.GachaBuyResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = GachaBuyRequest.FromBytes(payload.Span);
        var userId = session.User?.Id ?? session.UserId;
        var characterId = (int)session.CharacterId;
        var nico = request.BuyType == 2 && GachaTestSession.NicoPrice > 0;
        var price = nico ? GachaTestSession.NicoPrice : GachaTestSession.AiPrice;
        var prizeItemId = (int)GachaTestSession.PrizeItemId;
        logger.LogInformation(
            "GachaBuy from character {CharacterId} buyType={BuyType} price={Price} prize={Prize}",
            session.CharacterId,
            request.BuyType,
            price,
            prizeItemId
        );

        var user = session.User;
        var purse = nico ? user?.NicoPoints ?? 0 : user?.AiPoints ?? 0;
        if (user is null || characterId == 0 || purse < (long)price)
        {
            await session.SendAsync(ResponseType, new GachaBuyResponse(1, 0, 0, 0).ToBytes(), ct);
            return;
        }

        var character = await characterRepo.GetByIdAsync(characterId, ct);
        var previous =
            character?.Inventory.FirstOrDefault(i => i.ItemId == prizeItemId)?.Quantity ?? 0;
        if (!await characterRepo.AddInventoryAsync(characterId, prizeItemId, 1, ct))
        {
            await session.SendAsync(ResponseType, new GachaBuyResponse(1, 0, 0, 0).ToBytes(), ct);
            return;
        }

        var aiDelta = nico ? 0 : -(long)price;
        var nicoDelta = nico ? -(long)price : 0;
        var topped = await userRepo.AddMoneyAsync(userId, aiDelta, nicoDelta, ct);
        if (topped is not null)
        {
            session.User = topped;
            user = topped;
        }

        var serial = CharacterItemSync.ResolveSerialId(prizeItemId);
        var total = (ushort)Math.Clamp(previous + 1, 0, ushort.MaxValue);
        await session.SendAsync(
            ResponseType,
            new GachaBuyResponse(0, serial, 1, GachaTestSession.PrizeHitType).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.MoneyUpdatedAipoint,
            new MoneyUpdatedAipointNotify((ulong)Math.Max(0, user?.AiPoints ?? 0)).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.MoneyUpdatedNicopoint,
            new MoneyUpdatedNicopointNotify((ulong)Math.Max(0, user?.NicoPoints ?? 0)).ToBytes(),
            ct
        );
        await CharacterItemSync.SendInventoryItemAsync(session, prizeItemId, total, ct);
    }
}

public sealed class AreaGachaEndHandler(ILogger<AreaGachaEndHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GachaEndRequest;
    public PacketType ResponseType => PacketType.GachaEndResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("GachaEnd from character {CharacterId}", session.CharacterId);
        await session.SendAsync(ResponseType, new GachaEndResponse(0).ToBytes(), ct);
        await session.SendAsync(PacketType.GachaEndedNotify, new GachaEndedNotify().ToBytes(), ct);
    }
}

public sealed class AreaGachaTicketExchangeCloseHandler
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GachaTicketExchangeCloseRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    ) => Task.CompletedTask;
}
