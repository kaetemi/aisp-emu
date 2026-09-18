using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary><c>recv_gacha_end_r</c> (0x1380). UInt result.</summary>
public sealed class GachaEndResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
