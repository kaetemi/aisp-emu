using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>recv_gacha_buy_r</c> (0x2F2D), 11 bytes. Result 0 with a nonzero SerialId and Num stores the prize and
/// plays the capsule animation (kuzi_window01/02); result 0 with either at zero is a miss with no prize UI.
/// A nonzero result restores the displayed wallet and shows that value as a client message id.
/// The item itself still has to arrive through the regular item table notifies.
/// </summary>
public sealed class GachaBuyResponse(uint result, uint serialId, ushort num, byte hitType)
    : IOutgoingPacket
{
    public const int WireSize = 11;

    public uint Result { get; } = result;
    public uint SerialId { get; } = serialId;
    public ushort Num { get; } = num;
    public byte HitType { get; } = hitType;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(SerialId);
        writer.Write(Num);
        writer.Write(HitType);
        return writer.ToBytes();
    }

    public static GachaBuyResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GachaBuyResponse(
            reader.ReadUInt(),
            reader.ReadUInt(),
            reader.ReadUShort(),
            reader.ReadByte()
        );
    }
}
