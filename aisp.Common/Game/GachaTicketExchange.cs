using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

/// <summary>
/// The recycle box next to the capsule machine (アイポンボックス). The original service let Hotaru turn three
/// 500 P aiぽん products into one aiポンチケット; here the box attached to a machine takes that machine's own
/// prize items, <see cref="GachaMachine.RecycleItemsPerTicket"/> per ticket, and mints
/// <see cref="GachaMachine.TicketItemId"/> when it is settled.
/// </summary>
public sealed class GachaTicketExchange(
    MainContext db,
    IGachaRepository gachaRepository,
    ILogger<GachaTicketExchange> logger
)
{
    /// <summary>The client shows at most this many stacks in the box (kind-2 item list, 0x497505).</summary>
    public const int MaxStacks = 10;

    /// <summary>Opens the box for the session's machine; null when the machine has no recycle rule.</summary>
    public async Task<GachaTicketBox?> OpenAsync(IPlayerSession session, CancellationToken ct)
    {
        if (session.GachaTicketBox is { } open && open.MachineId == session.ActiveGachaMachineId)
            return open;

        if (session.ActiveGachaMachineId is not int machineId)
            return null;
        var machine = await gachaRepository.GetEnabledMachineAsync(machineId, ct);
        if (machine is null || machine.RecycleItemsPerTicket <= 0 || machine.TicketItemId is null)
            return null;

        session.GachaTicketBox = new GachaTicketBox
        {
            MachineId = machine.Id,
            TicketItemId = machine.TicketItemId.Value,
            ItemsPerTicket = machine.RecycleItemsPerTicket,
        };
        return session.GachaTicketBox;
    }

    /// <summary>Drops a bag stack into the box. False when the client should be refused.</summary>
    public async Task<bool> DepositAsync(
        IPlayerSession session,
        int itemId,
        int quantity,
        CancellationToken ct
    )
    {
        if (session.CharacterId == 0 || itemId <= 0 || quantity <= 0)
            return false;
        var box = await OpenAsync(session, ct);
        if (box is null)
            return false;
        if (itemId == box.TicketItemId)
            return false;

        var machine = await gachaRepository.GetEnabledMachineAsync(box.MachineId, ct);
        if (machine is null || !machine.Prizes.Any(p => p.ItemId == itemId))
        {
            logger.LogInformation(
                "GachaTicketExchange: character {CharacterId} offered item {ItemId}, not a prize of machine {MachineId}",
                session.CharacterId,
                itemId,
                box.MachineId
            );
            return false;
        }

        var deposited = box.Deposits.GetValueOrDefault(itemId);
        if (deposited == 0 && box.Deposits.Count >= MaxStacks)
            return false;
        var owned = await OwnedAsync(session, itemId, ct);
        if (owned - deposited < quantity)
            return false;

        // The client's box window keeps its own offered/remaining bookkeeping; a bag update here would be
        // subtracted twice. The bag is re-synced from the database when the box settles.
        var ticketsBefore = box.TicketsReady;
        box.Deposits[itemId] = deposited + quantity;
        await ShowPendingTicketsAsync(session, box, ticketsBefore, ct);
        return true;
    }

    /// <summary>Takes a stack back out of the box (とりだす). False when nothing like it is deposited.</summary>
    public async Task<bool> WithdrawAsync(
        IPlayerSession session,
        int itemId,
        int quantity,
        CancellationToken ct
    )
    {
        if (session.GachaTicketBox is not { } box || quantity <= 0)
            return false;
        var deposited = box.Deposits.GetValueOrDefault(itemId);
        if (deposited < quantity)
            return false;

        var ticketsBefore = box.TicketsReady;
        if (deposited == quantity)
            box.Deposits.Remove(itemId);
        else
            box.Deposits[itemId] = deposited - quantity;
        await ShowPendingTicketsAsync(session, box, ticketsBefore, ct);
        return true;
    }

    /// <summary>
    /// The exchange runs in the trade window, so the tickets the box would pay out are shown on the partner's
    /// side with the ordinary trade notifies: the old count comes off, the new count goes on.
    /// </summary>
    private static async Task ShowPendingTicketsAsync(
        IPlayerSession session,
        GachaTicketBox box,
        int ticketsBefore,
        CancellationToken ct
    )
    {
        var ticketsNow = box.TicketsReady;
        if (ticketsNow == ticketsBefore)
            return;
        var serial = CharacterItemSync.ResolveSerialId(box.TicketItemId);
        if (ticketsBefore > 0)
            await session.SendAsync(
                PacketType.TradeItemRemovedNotify,
                new TradeItemRemovedNotify(
                    serial,
                    (ushort)Math.Min(ticketsBefore, ushort.MaxValue)
                ).ToBytes(),
                ct
            );
        if (ticketsNow > 0)
            await session.SendAsync(
                PacketType.TradeItemAddedNotify,
                new TradeItemAddedNotify(
                    serial,
                    (ushort)Math.Min(ticketsNow, ushort.MaxValue)
                ).ToBytes(),
                ct
            );
    }

    /// <summary>
    /// Closes the box. When confirmed, every complete set of <see cref="GachaTicketBox.ItemsPerTicket"/>
    /// deposited items is taken from the bag and paid out as one ticket and the rest goes back; a cancelled
    /// box just forgets its deposits (nothing had left the bag). Returns the number of tickets minted.
    /// </summary>
    public async Task<int> SettleAsync(IPlayerSession session, bool confirmed, CancellationToken ct)
    {
        if (session.GachaTicketBox is not { } box)
            return 0;
        session.GachaTicketBox = null;
        if (session.CharacterId == 0 || box.Deposits.Count == 0)
            return 0;
        if (!confirmed)
        {
            logger.LogInformation(
                "GachaTicketExchange: character {CharacterId} cancelled the box with {Count} items in it",
                session.CharacterId,
                box.Total
            );
            return 0;
        }

        var characterId = checked((int)session.CharacterId);
        var tickets = box.TicketsReady;
        var ticketStackExisted = false;
        var toConsume = tickets * box.ItemsPerTicket;
        var consumed = new Dictionary<int, int>();
        foreach (var (itemId, quantity) in box.Deposits)
        {
            if (toConsume == 0)
                break;
            var take = Math.Min(quantity, toConsume);
            consumed[itemId] = take;
            toConsume -= take;
        }

        if (tickets > 0)
        {
            var stacks = await db
                .CharacterInventories.Where(x =>
                    x.CharacterId == characterId && consumed.Keys.Contains(x.ItemId)
                )
                .ToListAsync(ct);
            if (stacks.Count != consumed.Count || stacks.Any(s => s.Quantity < consumed[s.ItemId]))
            {
                logger.LogWarning(
                    "GachaTicketExchange: bag of character {CharacterId} no longer holds the deposited items; returning the box",
                    session.CharacterId
                );
                tickets = 0;
            }
            else
            {
                foreach (var stack in stacks)
                {
                    stack.Quantity -= consumed[stack.ItemId];
                    if (stack.Quantity == 0)
                        db.CharacterInventories.Remove(stack);
                }
                var ticketStack = await db.CharacterInventories.SingleOrDefaultAsync(
                    x => x.CharacterId == characterId && x.ItemId == box.TicketItemId,
                    ct
                );
                ticketStackExisted = ticketStack is not null;
                if (ticketStack is null)
                    db.CharacterInventories.Add(
                        new CharacterInventory
                        {
                            CharacterId = characterId,
                            ItemId = box.TicketItemId,
                            Quantity = tickets,
                        }
                    );
                else
                    ticketStack.Quantity += tickets;

                if (!await CharacterInventoryRepository.TrySaveChangesAsync(db, ct))
                {
                    logger.LogInformation(
                        "GachaTicketExchange: bag of character {CharacterId} is full; returning the box",
                        session.CharacterId
                    );
                    tickets = 0;
                }
            }
        }

        // Re-sync every stack the box touched from what the bag really holds now; the client's window
        // undoes its own offered-quantity bookkeeping when it closes.
        foreach (var itemId in box.Deposits.Keys)
            await CharacterItemSync.SendInventoryQuantityAsync(
                session,
                itemId,
                await OwnedAsync(session, itemId, ct),
                ct
            );
        if (tickets > 0)
        {
            // A stack the bag already had gets a count update; recv_item_create for it would re-slot the cell.
            var ticketsOwned = await OwnedAsync(session, box.TicketItemId, ct);
            if (ticketStackExisted)
                await CharacterItemSync.SendInventoryQuantityAsync(
                    session,
                    box.TicketItemId,
                    ticketsOwned,
                    ct
                );
            else
                await CharacterItemSync.SendInventoryItemAsync(
                    session,
                    box.TicketItemId,
                    (ushort)Math.Min(ticketsOwned, ushort.MaxValue),
                    ct
                );
            logger.LogInformation(
                "GachaTicketExchange: character {CharacterId} recycled {Count} items into {Tickets} x ticket {TicketItemId}",
                session.CharacterId,
                consumed.Values.Sum(),
                tickets,
                box.TicketItemId
            );
        }
        return tickets;
    }

    private async Task<int> OwnedAsync(IPlayerSession session, int itemId, CancellationToken ct)
    {
        var characterId = checked((int)session.CharacterId);
        return await db
            .CharacterInventories.AsNoTracking()
            .Where(x => x.CharacterId == characterId && x.ItemId == itemId)
            .Select(x => x.Quantity)
            .FirstOrDefaultAsync(ct);
    }
}
