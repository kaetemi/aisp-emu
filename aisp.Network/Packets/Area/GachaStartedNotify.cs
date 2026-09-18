using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>recv_gacha_started</c> (0xCC88). Opens <c>CGachaWindow</c> (PAS <c>kuzi_window00.xml</c>, live name aiぽん).
/// Reader is CString name (max 0xC1 including NUL), then visual, D price, P price, catalog serial/qty.
/// </summary>
public sealed class GachaStartedNotify(
    string name,
    uint visualId,
    ulong aiPoint,
    ulong nicoPoint,
    uint itemSerialId = 0,
    ushort itemNum = 0
) : IOutgoingPacket
{
    public const int NameMaxBytesIncludingNul = 0xC1;

    public string Name { get; } = name;
    public uint VisualId { get; } = visualId;
    public ulong AiPoint { get; } = aiPoint;
    public ulong NicoPoint { get; } = nicoPoint;
    public uint ItemSerialId { get; } = itemSerialId;
    public ushort ItemNum { get; } = itemNum;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Name, NameMaxBytesIncludingNul - 1);
        writer.Write(VisualId);
        writer.Write(AiPoint);
        writer.Write(NicoPoint);
        writer.Write(ItemSerialId);
        writer.Write(ItemNum);
        return writer.ToBytes();
    }

    public static GachaStartedNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GachaStartedNotify(
            reader.ReadString(),
            reader.ReadUInt(),
            reader.ReadULong(),
            reader.ReadULong(),
            reader.ReadUInt(),
            reader.ReadUShort()
        );
    }
}
