using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>recv_gacha_buy_r</c> (0x2F2D). 11 bytes: result, serial, qty, hit-type.
/// Result 0 and SerialId/Num both nonzero stores the prize and runs the capsule anim.
/// Result 0 with serial or num 0 is success with no prize UI.
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
