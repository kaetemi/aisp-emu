using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// とりだす on the capsule machine window: moves one item box entry to the bag. The client sends the first
/// entry of its list and, on result 0, immediately asks for the next one, so the entry is removed from its
/// list (<c>recv_itembox_item_delete</c>) before the result goes out. A full bag answers 1, which stops the
/// chain and leaves the entry in the box.
/// </summary>
public sealed class AreaItemboxTakeoutHandler(
    MainContext db,
    ICharacterRepository characters,
    IItemboxRepository itembox,
    ILogger<AreaItemboxTakeoutHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public const int RefusedResult = 1;

    public PacketType RequestType => PacketType.ItemBoxTakeoutRequest;
    public PacketType ResponseType => PacketType.ItemBoxTakeoutResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = ItemBoxTakeoutRequest.FromBytes(payload.Span);
        if (session.CharacterId == 0 || request.EntryId > long.MaxValue)
        {
            await RefuseAsync(session, request.EntryId, "no character or bad entry id", ct);
            return;
        }

        var characterId = checked((int)session.CharacterId);
        var entry = await itembox.FindAsync(characterId, (long)request.EntryId, ct);
        if (entry is null)
        {
            await RefuseAsync(session, request.EntryId, "entry is not in this character's box", ct);
            return;
        }

        var stackExisted = await db
            .CharacterInventories.AsNoTracking()
            .AnyAsync(
                x => x.CharacterId == characterId && x.ItemId == entry.ItemId && x.Quantity > 0,
                ct
            );

        // One SaveChanges for the removal and the bag stack: a full bag clears both and the entry stays boxed.
        db.ItemboxEntries.Remove(entry);
        if (!await characters.AddInventoryAsync(characterId, entry.ItemId, entry.Quantity, ct))
        {
            await RefuseAsync(session, request.EntryId, "inventory is full", ct);
            return;
        }

        var owned = await db
            .CharacterInventories.AsNoTracking()
            .Where(x => x.CharacterId == characterId && x.ItemId == entry.ItemId)
            .Select(x => x.Quantity)
            .FirstOrDefaultAsync(ct);

        logger.LogInformation(
            "ItemboxTakeout: character {CharacterId} took entry {EntryId} (item {ItemId} x{Quantity}) to the bag",
            session.CharacterId,
            entry.Id,
            entry.ItemId,
            entry.Quantity
        );

        // A new stack arrives as recv_item_create, an existing one only gets its count bumped (a second
        // create would re-slot the bag cell).
        if (stackExisted)
            await CharacterItemSync.SendInventoryQuantityAsync(session, entry.ItemId, owned, ct);
        else
            await CharacterItemSync.SendInventoryItemAsync(
                session,
                entry.ItemId,
                (ushort)Math.Min(owned, ushort.MaxValue),
                ct
            );
        await Itembox.SendEntryRemovedAsync(session, entry.Id, ct);
        await session.SendAsync(ResponseType, new ItemBoxTakeoutResponse(0).ToBytes(), ct);
    }

    private Task RefuseAsync(
        IPlayerSession session,
        ulong entryId,
        string reason,
        CancellationToken ct
    )
    {
        logger.LogInformation(
            "ItemboxTakeout refused for character {CharacterId} (entry {EntryId}): {Reason}",
            session.CharacterId,
            entryId,
            reason
        );
        return session.SendAsync(
            ResponseType,
            new ItemBoxTakeoutResponse(RefusedResult).ToBytes(),
            ct
        );
    }
}
