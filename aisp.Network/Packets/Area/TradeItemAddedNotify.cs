using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>recv_trade_item_added</c> (0x2F29): the trade partner put a stack on their side of the trade window.
/// The NPC ticket exchange reuses the same window, so this also shows the tickets Hotaru will hand over.
/// </summary>
public sealed class TradeItemAddedNotify(uint serialId, ushort num) : IOutgoingPacket
{
    public uint SerialId { get; } = serialId;
    public ushort Num { get; } = num;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(SerialId);
        writer.Write(Num);
        return writer.ToBytes();
    }

    public static TradeItemAddedNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new TradeItemAddedNotify(reader.ReadUInt(), reader.ReadUShort());
    }
}
