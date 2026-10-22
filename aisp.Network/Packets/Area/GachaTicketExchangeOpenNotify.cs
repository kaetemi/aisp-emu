using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>recv_gachaticket_exchange_open</c> (0x7D14). Opens the standalone ガチャガチャボックス
/// (kuzi_item00.xml); BaseNum is the deposit quota shown as 000/BaseNum.
/// </summary>
public sealed class GachaTicketExchangeOpenNotify(uint baseNum) : IOutgoingPacket
{
    public uint BaseNum { get; } = baseNum;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(BaseNum);
        return writer.ToBytes();
    }

    public static GachaTicketExchangeOpenNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GachaTicketExchangeOpenNotify(reader.ReadUInt());
    }
}
