using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary><c>recv_gachaticket_exchange_item_add_r</c> (0x7F77). UInt result; the box only draws the stack on 0.</summary>
public sealed class GachaTicketExchangeItemAddResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
