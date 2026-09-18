namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>send_gachaticket_exchange_item_add</c> (0x45BD). Dump serial/qty into ガチャガチャボックス.
/// </summary>
public sealed class GachaTicketExchangeItemAddRequest : IIncomingPacket<GachaTicketExchangeItemAddRequest>
{
    public uint SerialId { get; init; }
    public ushort Num { get; init; }

    public static GachaTicketExchangeItemAddRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GachaTicketExchangeItemAddRequest { SerialId = reader.ReadUInt(), Num = reader.ReadUShort() };
    }
}
