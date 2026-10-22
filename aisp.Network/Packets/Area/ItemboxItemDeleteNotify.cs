namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>recv_itembox_item_delete</c> (0x1BFE), 8 bytes. Removes one entry from the client's item box list.
/// Must precede <c>recv_itembox_takeout_r</c> 0 for the entry just taken out, because that result makes the
/// client send the takeout for whatever entry is first in its list.
/// </summary>
public sealed class ItemboxItemDeleteNotify(ulong entryId) : IOutgoingPacket
{
    public const int WireSize = 8;

    public ulong EntryId { get; } = entryId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(EntryId);
        return writer.ToBytes();
    }

    public static ItemboxItemDeleteNotify FromBytes(ReadOnlySpan<byte> data) =>
        new(new PacketReader(data).ReadULong());
}
