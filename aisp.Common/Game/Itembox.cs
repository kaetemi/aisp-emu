using aisp.Common.DAL.Entities;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Game;

/// <summary>
/// The character's item box as the 2011 client knows it: a list in its item manager, drawn by the capsule
/// machine window as ガチャガチャボックス with a counter the client formats against a hard-coded 100. The list
/// persists on the client across windows, so a machine open re-sends the box as delete + create pairs to
/// end up with exactly one copy of every entry whatever the client already had.
/// </summary>
public static class Itembox
{
    /// <summary>Slots the client window counts against; the number is compiled into the client.</summary>
    public const int Capacity = 100;

    public static ItemboxItemCreateNotify ToCreateNotify(ItemboxEntry entry) =>
        new(
            checked((ulong)entry.Id),
            CharacterItemSync.ResolveSerialId(entry.ItemId),
            (ushort)Math.Clamp(entry.Quantity, 0, ushort.MaxValue)
        );

    public static Task SendEntryAsync(
        IPlayerSession session,
        ItemboxEntry entry,
        CancellationToken ct
    ) => session.SendAsync(PacketType.ItemboxItemCreateNotify, ToCreateNotify(entry).ToBytes(), ct);

    public static Task SendEntryRemovedAsync(
        IPlayerSession session,
        long entryId,
        CancellationToken ct
    ) =>
        session.SendAsync(
            PacketType.ItemboxItemDeleteNotify,
            new ItemboxItemDeleteNotify(checked((ulong)entryId)).ToBytes(),
            ct
        );

    public static async Task SendContentsAsync(
        IPlayerSession session,
        IReadOnlyList<ItemboxEntry> entries,
        CancellationToken ct
    )
    {
        foreach (var entry in entries)
        {
            await SendEntryRemovedAsync(session, entry.Id, ct);
            await SendEntryAsync(session, entry, ct);
        }
    }
}
