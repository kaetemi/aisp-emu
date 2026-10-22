using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// One turn of the handle on the machine the session opened (<see cref="IPlayerSession.ActiveGachaMachineId"/>).
/// Charges the machine's price in the currency the client chose, draws a weighted prize, parks it in the
/// character's item box (the ガチャガチャボックス panel of the machine window, see <see cref="Itembox"/>) and
/// answers <c>recv_gacha_buy_r</c>, the wallet or ticket notify and <c>recv_itembox_item_create</c>.
/// Charge and box entry go through one SaveChanges; a full box refuses the pull without taking the money.
/// </summary>
public sealed class AreaGachaBuyHandler(
    MainContext db,
    IGachaRepository gachaRepository,
    IItemboxRepository itembox,
    ILogger<AreaGachaBuyHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    /// <summary>
    /// A nonzero result makes the client restore its displayed wallet and show 「エラーコード：N」 with the
    /// value; it does not interpret individual codes, so one generic value is used.
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
            await RefuseAsync(session, "malformed request", ct);
            return;
        }

        if (session.CharacterId == 0 || session.User is null)
        {
            await RefuseAsync(session, "no character", ct);
            return;
        }

        if (session.ActiveGachaMachineId is not int machineId)
        {
            await RefuseAsync(session, "no machine open", ct);
            return;
        }

        var machine = await gachaRepository.GetEnabledMachineAsync(machineId, ct);
        if (machine is null)
        {
            await RefuseAsync(session, $"machine {machineId} is gone", ct);
            return;
        }

        var prize = GachaRepository.Draw(machine, Random.Shared);
        if (prize is null)
        {
            await RefuseAsync(session, $"machine {machine.Code} has no prizes", ct);
            return;
        }

        var characterId = checked((int)session.CharacterId);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == session.User.Id, ct);
        if (user is null)
        {
            await RefuseAsync(session, "user is gone", ct);
            return;
        }

        var boxed = await itembox.CountAsync(characterId, ct);
        if (boxed >= Itembox.Capacity)
        {
            await RefuseAsync(session, $"item box is full ({boxed}/{Itembox.Capacity})", ct);
            return;
        }

        // A ticket-only machine (no D or P price) is priced in tickets by the client, which still reports the
        // turn as the D kind it was opened with; only a ticket inserted next to a priced machine arrives as 2.
        var payment = request.BuyType;
        if (
            payment == GachaBuyType.AiPoints
            && machine.AiPrice <= 0
            && machine.NicoPrice <= 0
            && machine.TicketItemId is not null
        )
            payment = GachaBuyType.Ticket;

        var ticketRemaining = 0;
        switch (payment)
        {
            case GachaBuyType.AiPoints:
                if (machine.AiPrice <= 0)
                {
                    await RefuseAsync(session, $"machine {machine.Code} has no D price", ct);
                    return;
                }
                if (user.AiPoints < machine.AiPrice)
                {
                    await RefuseAsync(session, $"{user.AiPoints} D < {machine.AiPrice} D", ct);
                    return;
                }
                user.AiPoints -= machine.AiPrice;
                break;

            case GachaBuyType.NicoPoints:
                if (machine.NicoPrice <= 0)
                {
                    await RefuseAsync(session, $"machine {machine.Code} has no P price", ct);
                    return;
                }
                if (user.NicoPoints < machine.NicoPrice)
                {
                    await RefuseAsync(session, $"{user.NicoPoints} P < {machine.NicoPrice} P", ct);
                    return;
                }
                user.NicoPoints -= machine.NicoPrice;
                break;

            case GachaBuyType.Ticket:
                if (machine.TicketItemId is not int ticketItemId || machine.TicketsPerPull <= 0)
                {
                    await RefuseAsync(session, $"machine {machine.Code} takes no tickets", ct);
                    return;
                }
                var tickets = await db.CharacterInventories.SingleOrDefaultAsync(
                    x => x.CharacterId == characterId && x.ItemId == ticketItemId,
                    ct
                );
                if (tickets is null || tickets.Quantity < machine.TicketsPerPull)
                {
                    await RefuseAsync(
                        session,
                        $"{tickets?.Quantity ?? 0} x ticket {ticketItemId} < {machine.TicketsPerPull}",
                        ct
                    );
                    return;
                }
                tickets.Quantity -= machine.TicketsPerPull;
                ticketRemaining = tickets.Quantity;
                if (tickets.Quantity == 0)
                    db.CharacterInventories.Remove(tickets);
                break;

            default:
                await RefuseAsync(session, $"unknown buy type {request.BuyType}", ct);
                return;
        }

        var entry = new ItemboxEntry
        {
            CharacterId = characterId,
            ItemId = prize.ItemId,
            Quantity = prize.Quantity,
        };
        db.ItemboxEntries.Add(entry);
        if (!await CharacterInventoryRepository.TrySaveChangesAsync(db, ct))
        {
            await RefuseAsync(session, "could not save the pull", ct);
            return;
        }

        session.User.AiPoints = user.AiPoints;
        session.User.NicoPoints = user.NicoPoints;

        logger.LogInformation(
            "GachaBuy: character {CharacterId} pulled item {ItemId} x{Quantity} from {Code} with {BuyType} into box entry {EntryId} ({Boxed}/{Capacity})",
            session.CharacterId,
            prize.ItemId,
            prize.Quantity,
            machine.Code,
            payment,
            entry.Id,
            boxed + 1,
            Itembox.Capacity
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
        switch (payment)
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
        // The capsule window only needs the prize for its result popup (it came with buy_r); the item itself
        // appears in the box panel, and reaches the bag when the player takes it out.
        await Itembox.SendEntryAsync(session, entry, ct);
    }

    private Task RefuseAsync(IPlayerSession session, string reason, CancellationToken ct)
    {
        logger.LogInformation(
            "GachaBuy refused for character {CharacterId}: {Reason}",
            session.CharacterId,
            reason
        );
        return session.SendAsync(
            ResponseType,
            new GachaBuyResponse(RefusedResult, 0, 0, 0).ToBytes(),
            ct
        );
    }
}

/// <summary>Close button of an idle machine: settle the recycle box, acknowledge and tell the client the session is over.</summary>
public sealed class AreaGachaEndHandler(
    GachaTicketExchange ticketExchange,
    ILogger<AreaGachaEndHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
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
        await ticketExchange.SettleAsync(session, confirmed: true, ct);
        session.ActiveGachaMachineId = null;
        await session.SendAsync(ResponseType, new GachaEndResponse(0).ToBytes(), ct);
        await session.SendAsync(PacketType.GachaEndedNotify, new GachaEndedNotify().ToBytes(), ct);
    }
}

/// <summary>
/// A bag stack dropped into the アイポンボックス. The client draws it in the box only on result 0 and keeps
/// its own deposited-quantity counter; the bag side is kept honest with recv_item_update_num.
/// </summary>
public sealed class AreaGachaTicketExchangeItemAddHandler(
    GachaTicketExchange ticketExchange,
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
        var accepted =
            request.SerialId <= int.MaxValue
            && await ticketExchange.DepositAsync(session, (int)request.SerialId, request.Num, ct);
        logger.LogInformation(
            "GachaTicketExchange add from character {CharacterId}: serial={SerialId} num={Num} → {Result}",
            session.CharacterId,
            request.SerialId,
            request.Num,
            accepted ? "accepted" : "refused"
        );
        await session.SendAsync(
            ResponseType,
            new GachaTicketExchangeItemAddResponse(accepted ? 0 : RefusedResult).ToBytes(),
            ct
        );
    }
}

/// <summary>とりだす: a deposited stack goes back to the bag.</summary>
public sealed class AreaGachaTicketExchangeItemDelHandler(
    GachaTicketExchange ticketExchange,
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
        var accepted =
            request.SerialId <= int.MaxValue
            && await ticketExchange.WithdrawAsync(session, (int)request.SerialId, request.Num, ct);
        logger.LogInformation(
            "GachaTicketExchange del from character {CharacterId}: serial={SerialId} num={Num} → {Result}",
            session.CharacterId,
            request.SerialId,
            request.Num,
            accepted ? "accepted" : "refused"
        );
        await session.SendAsync(
            ResponseType,
            new GachaTicketExchangeItemDelResponse(accepted ? 0 : RefusedResult).ToBytes(),
            ct
        );
    }
}

/// <summary>
/// <c>send_gachaticket_exchange_close</c> has no reply on the wire. Reason 0 is the confirmed hand-over
/// (トレード on the box window) and settles the box: complete sets become tickets, the rest returns to the
/// bag. Any other reason is a cancel and the deposits are simply forgotten.
/// </summary>
public sealed class AreaGachaTicketExchangeCloseHandler(
    GachaTicketExchange ticketExchange,
    ILogger<AreaGachaTicketExchangeCloseHandler> logger,
    ServerScriptDispatcher? serverScriptDispatcher = null
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public const uint ConfirmedReason = 0;

    public PacketType RequestType => PacketType.GachaTicketExchangeCloseRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = GachaTicketExchangeCloseRequest.FromBytes(payload.Span);
        var confirmed = request.Reason == ConfirmedReason;
        var tickets = await ticketExchange.SettleAsync(session, confirmed, ct);
        logger.LogInformation(
            "GachaTicketExchange close from character {CharacterId}: reason={Reason} tickets={Tickets}",
            session.CharacterId,
            request.Reason,
            tickets
        );
        // A box opened by an NPC script (Hotaru) continues its event from here.
        HotaruServerScript.RecordSettlement(session, confirmed, tickets);
        if (serverScriptDispatcher is not null)
            await serverScriptDispatcher.TryHandlePacketAsync(RequestType, payload, session, ct);
    }
}
