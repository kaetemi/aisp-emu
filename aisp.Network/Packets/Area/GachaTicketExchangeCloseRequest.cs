using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary><c>send_gachaticket_exchange_close</c> (0x953B). Closes the ticket box; the client expects no reply.</summary>
public sealed class GachaTicketExchangeCloseRequest
    : IIncomingPacket<GachaTicketExchangeCloseRequest>
{
    public uint Reason { get; init; }

    public static GachaTicketExchangeCloseRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GachaTicketExchangeCloseRequest { Reason = reader.ReadUInt() };
    }
}
