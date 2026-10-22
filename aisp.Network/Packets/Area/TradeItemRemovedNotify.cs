using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary><c>recv_trade_item_removed</c> (0x0898): the trade partner took a stack back off their side.</summary>
public sealed class TradeItemRemovedNotify(uint serialId, ushort num) : IOutgoingPacket
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

    public static TradeItemRemovedNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new TradeItemRemovedNotify(reader.ReadUInt(), reader.ReadUShort());
    }
}
