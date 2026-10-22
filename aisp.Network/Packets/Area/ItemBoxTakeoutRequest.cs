namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>send_itembox_takeout</c> (0xBBE3), 8 bytes. とりだす on the capsule machine window: the client sends
/// the id of the first entry in its item box list and waits for <c>recv_itembox_takeout_r</c>.
/// </summary>
public sealed class ItemBoxTakeoutRequest(ulong entryId) : IIncomingPacket<ItemBoxTakeoutRequest>
{
    public ulong EntryId { get; } = entryId;

    public static ItemBoxTakeoutRequest FromBytes(ReadOnlySpan<byte> data) =>
        new(new PacketReader(data).ReadULong());
}
