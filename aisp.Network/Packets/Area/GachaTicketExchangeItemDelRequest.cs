namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>send_gachaticket_exchange_item_del</c> (0x3E26). Return serial/qty from ガチャガチャボックス to the bag.
/// </summary>
public sealed class GachaTicketExchangeItemDelRequest : IIncomingPacket<GachaTicketExchangeItemDelRequest>
{
    public uint SerialId { get; init; }
    public ushort Num { get; init; }

    public static GachaTicketExchangeItemDelRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GachaTicketExchangeItemDelRequest { SerialId = reader.ReadUInt(), Num = reader.ReadUShort() };
    }
}
