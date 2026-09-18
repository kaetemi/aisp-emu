using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary><c>recv_gachaticket_exchange_item_del_r</c> (0xC6DA). UInt result.</summary>
public sealed class GachaTicketExchangeItemDelResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }

    public static GachaTicketExchangeItemDelResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GachaTicketExchangeItemDelResponse(reader.ReadUInt());
    }
}
