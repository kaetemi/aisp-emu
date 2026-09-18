using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary><c>recv_gachaticket_exchange_item_add_r</c> (0x7F77). UInt result.</summary>
public sealed class GachaTicketExchangeItemAddResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }

    public static GachaTicketExchangeItemAddResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GachaTicketExchangeItemAddResponse(reader.ReadUInt());
    }
}
