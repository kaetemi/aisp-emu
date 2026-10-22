namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>recv_itembox_item_create</c> (0x8782), 22 bytes. One entry of the character's item box, the 100-slot
/// holding area drawn in the capsule machine window (ガチャガチャボックス). The client appends it to its item box
/// list and redraws the grid and the n/100 counter. <see cref="EntryId"/> is what <c>send_itembox_takeout</c>
/// and <c>recv_itembox_item_delete</c> refer to; <see cref="Extra"/> is a trailing 64-bit field the 2011
/// client stores but never reads.
/// </summary>
public sealed class ItemboxItemCreateNotify(
    ulong entryId,
    uint serialId,
    ushort num,
    ulong extra = 0
) : IOutgoingPacket
{
    public const int WireSize = 22;

    public ulong EntryId { get; } = entryId;
    public uint SerialId { get; } = serialId;
    public ushort Num { get; } = num;
    public ulong Extra { get; } = extra;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(EntryId);
        writer.Write(SerialId);
        writer.Write(Num);
        writer.Write(Extra);
        return writer.ToBytes();
    }

    public static ItemboxItemCreateNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new ItemboxItemCreateNotify(
            reader.ReadULong(),
            reader.ReadUInt(),
            reader.ReadUShort(),
            reader.ReadULong()
        );
    }
}
