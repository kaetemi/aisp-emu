namespace aisp.Network.Packets.Area;

/// <summary><c>send_gacha_end</c> (0xD7FC). Empty; close the machine from idle.</summary>
public sealed class GachaEndRequest : IIncomingPacket<GachaEndRequest>
{
    public static GachaEndRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
