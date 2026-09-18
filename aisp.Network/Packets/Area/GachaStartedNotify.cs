using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>recv_gacha_started</c> (0xCC88). Opens <c>CGachaWindow</c> (PAS <c>kuzi_window00.xml</c>, live name aiぽん).
/// VisualId loads <c>./interface/package/%08d.dds</c> (str_table 100,950,20) into PAS unit 130 ガチャ看板.
/// 100000 = MoonScape; 101000 DC店; 102000 CL店; 103000 SH店; 200001 Bellair furniture.
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
