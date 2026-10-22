using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// One turn of the handle on the machine the session opened (<see cref="IPlayerSession.ActiveGachaMachineId"/>).
/// Charges the machine's price in the currency the client chose, draws a weighted prize, puts it in the bag and
/// answers <c>recv_gacha_buy_r</c> followed by the wallet and item-table notifies the capsule animation reads.
/// Charge and grant go through one SaveChanges so a full bag refuses the pull without taking the money.
/// </summary>
public sealed class AreaGachaBuyHandler(
    MainContext db,
    ICharacterRepository characters,
    IGachaRepository gachaRepository,
    ILogger<AreaGachaBuyHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    /// <summary>
    /// Nonzero results are shown by the client as a message id; the machine does not interpret them
    /// beyond "no capsule". One generic value keeps the dialog predictable.
    /// </summary>
    public const uint RefusedResult = 1;

    public PacketType RequestType => PacketType.GachaBuyRequest;
    public PacketType ResponseType => PacketType.GachaBuyResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        GachaBuyRequest request;
        try
        {
            request = GachaBuyRequest.FromBytes(payload.Span);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to parse GachaBuyRequest for character {CharacterId}",
                session.CharacterId
            );
            await RefuseAsync(session, ct);
            return;
        }

        if (session.CharacterId == 0 || session.User is null)
        {
            await RefuseAsync(session, ct);
            return;
        }

        if (session.ActiveGachaMachineId is not int machineId)
        {
            logger.LogWarning(
                "GachaBuy refused for character {CharacterId}: no machine open",
                session.CharacterId
            );
            await RefuseAsync(session, ct);
            return;
        }

        var machine = await gachaRepository.GetEnabledMachineAsync(machineId, ct);
        if (machine is null)
        {
            logger.LogWarning(
                "GachaBuy refused for character {CharacterId}: machine {MachineId} is gone",
                session.CharacterId,
                machineId
            );
            await RefuseAsync(session, ct);
            return;
        }

        var prize = GachaRepository.Draw(machine, Random.Shared);
        if (prize is null)
        {
            logger.LogWarning(
                "GachaBuy refused for character {CharacterId}: machine {Code} has no prizes",
                session.CharacterId,
                machine.Code
            );
            await RefuseAsync(session, ct);
            return;
        }

        var characterId = checked((int)session.CharacterId);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == session.User.Id, ct);
        if (user is null)
        {
            await RefuseAsync(session, ct);
            return;
        }

        var ticketRemaining = 0;
        switch (request.BuyType)
        {
            case GachaBuyType.AiPoints:
                if (machine.AiPrice <= 0 || user.AiPoints < machine.AiPrice)
                {
                    await RefuseAsync(session, ct);
                    return;
                }
                user.AiPoints -= machine.AiPrice;
                break;

            case GachaBuyType.NicoPoints:
                if (machine.NicoPrice <= 0 || user.NicoPoints < machine.NicoPrice)
                {
                    await RefuseAsync(session, ct);
                    return;
                }
                user.NicoPoints -= machine.NicoPrice;
                break;

            case GachaBuyType.Ticket:
                if (machine.TicketItemId is not int ticketItemId || machine.TicketsPerPull <= 0)
                {
                    await RefuseAsync(session, ct);
                    return;
                }
                var tickets = await db.CharacterInventories.SingleOrDefaultAsync(
                    x => x.CharacterId == characterId && x.ItemId == ticketItemId,
                    ct
                );
                if (tickets is null || tickets.Quantity < machine.TicketsPerPull)
                {
                    await RefuseAsync(session, ct);
                    return;
                }
                tickets.Quantity -= machine.TicketsPerPull;
                ticketRemaining = tickets.Quantity;
                if (tickets.Quantity == 0)
                    db.CharacterInventories.Remove(tickets);
                break;

            default:
                logger.LogWarning(
                    "GachaBuy refused for character {CharacterId}: unknown buy type {BuyType}",
                    session.CharacterId,
                    request.BuyType
                );
                await RefuseAsync(session, ct);
                return;
        }

        // AddInventoryAsync saves the pending charge together with the new stack, or clears both when the bag is full.
        if (!await characters.AddInventoryAsync(characterId, prize.ItemId, prize.Quantity, ct))
        {
            logger.LogInformation(
                "GachaBuy refused for character {CharacterId}: inventory is full",
                session.CharacterId
            );
            await RefuseAsync(session, ct);
            return;
        }

        session.User.AiPoints = user.AiPoints;
        session.User.NicoPoints = user.NicoPoints;
        var owned = await db
            .CharacterInventories.AsNoTracking()
            .Where(x => x.CharacterId == characterId && x.ItemId == prize.ItemId)
            .Select(x => x.Quantity)
            .FirstOrDefaultAsync(ct);

        logger.LogInformation(
            "GachaBuy: character {CharacterId} pulled item {ItemId} x{Quantity} from {Code} with {BuyType}",
            session.CharacterId,
            prize.ItemId,
            prize.Quantity,
            machine.Code,
            request.BuyType
        );

        await session.SendAsync(
            ResponseType,
            new GachaBuyResponse(
                0,
                CharacterItemSync.ResolveSerialId(prize.ItemId),
                (ushort)prize.Quantity,
                (byte)prize.HitType
            ).ToBytes(),
            ct
        );
        switch (request.BuyType)
        {
            case GachaBuyType.AiPoints:
                await session.SendAsync(
                    PacketType.MoneyUpdatedAipoint,
                    new MoneyUpdatedAipointNotify((ulong)Math.Max(0, user.AiPoints)).ToBytes(),
                    ct
                );
                break;
            case GachaBuyType.NicoPoints:
                await session.SendAsync(
                    PacketType.MoneyUpdatedNicopoint,
                    new MoneyUpdatedNicopointNotify((ulong)Math.Max(0, user.NicoPoints)).ToBytes(),
                    ct
                );
                break;
            case GachaBuyType.Ticket:
                await CharacterItemSync.SendInventoryQuantityAsync(
                    session,
                    machine.TicketItemId!.Value,
                    ticketRemaining,
                    ct
                );
                break;
        }
        await CharacterItemSync.SendInventoryItemAsync(
            session,
            prize.ItemId,
            (ushort)Math.Min(owned, ushort.MaxValue),
            ct
        );
    }

    private Task RefuseAsync(IPlayerSession session, CancellationToken ct) =>
        session.SendAsync(ResponseType, new GachaBuyResponse(RefusedResult, 0, 0, 0).ToBytes(), ct);
}

/// <summary>Close button of an idle machine: acknowledge and tell the client the session is over.</summary>
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
        logger.LogInformation(
            "GachaEnd from character {CharacterId} (machine {MachineId})",
            session.CharacterId,
            session.ActiveGachaMachineId
        );
        session.ActiveGachaMachineId = null;
        await session.SendAsync(ResponseType, new GachaEndResponse(0).ToBytes(), ct);
        await session.SendAsync(PacketType.GachaEndedNotify, new GachaEndedNotify().ToBytes(), ct);
    }
}

/// <summary>
/// The アイポンボックス deposit panel next to the machine. Depositing items toward tickets is not
/// implemented, so stacks are refused (the client keeps them in the bag) rather than acknowledged
/// without moving anything.
/// </summary>
public sealed class AreaGachaTicketExchangeItemAddHandler(
    ILogger<AreaGachaTicketExchangeItemAddHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public const uint RefusedResult = 1;

    public PacketType RequestType => PacketType.GachaTicketExchangeItemAddRequest;
    public PacketType ResponseType => PacketType.GachaTicketExchangeItemAddResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = GachaTicketExchangeItemAddRequest.FromBytes(payload.Span);
        logger.LogInformation(
            "GachaTicketExchange add refused for character {CharacterId}: serial={SerialId} num={Num} (box not implemented)",
            session.CharacterId,
            request.SerialId,
            request.Num
        );
        await session.SendAsync(
            ResponseType,
            new GachaTicketExchangeItemAddResponse(RefusedResult).ToBytes(),
            ct
        );
    }
}

public sealed class AreaGachaTicketExchangeItemDelHandler(
    ILogger<AreaGachaTicketExchangeItemDelHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public const uint RefusedResult = 1;

    public PacketType RequestType => PacketType.GachaTicketExchangeItemDelRequest;
    public PacketType ResponseType => PacketType.GachaTicketExchangeItemDelResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = GachaTicketExchangeItemDelRequest.FromBytes(payload.Span);
        logger.LogInformation(
            "GachaTicketExchange del refused for character {CharacterId}: serial={SerialId} num={Num} (box not implemented)",
            session.CharacterId,
            request.SerialId,
            request.Num
        );
        await session.SendAsync(
            ResponseType,
            new GachaTicketExchangeItemDelResponse(RefusedResult).ToBytes(),
            ct
        );
    }
}

/// <summary><c>send_gachaticket_exchange_close</c> has no reply on the wire; consume it.</summary>
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
