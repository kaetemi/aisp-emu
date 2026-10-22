namespace aisp.Network.Packets.Area;

/// <summary><c>recv_gacha_ended</c> (0x78A8). The server closed the machine. Empty.</summary>
public sealed class GachaEndedNotify : IOutgoingPacket
{
    public byte[] ToBytes() => [];
}
