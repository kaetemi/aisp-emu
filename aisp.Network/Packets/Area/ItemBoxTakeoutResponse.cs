namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>recv_itembox_takeout_r</c> (0x4135), 4 bytes. Result 0 makes the client send <c>send_itembox_takeout</c>
/// for the entry now first in its list, so one とりだす click drains the whole box one round trip at a time;
/// with an empty list or any other result it just refreshes the machine window.
/// </summary>
public sealed class ItemBoxTakeoutResponse(int result) : IOutgoingPacket
{
    public const int WireSize = 4;

    public int Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }

    public static ItemBoxTakeoutResponse FromBytes(ReadOnlySpan<byte> data) =>
        new((int)new PacketReader(data).ReadUInt());
}
