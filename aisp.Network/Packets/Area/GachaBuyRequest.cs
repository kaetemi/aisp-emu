using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>send_gacha_buy</c> (0x663A). One turn of the handle; the machine is the one the session opened.
/// </summary>
public sealed class GachaBuyRequest : IIncomingPacket<GachaBuyRequest>
{
    public GachaBuyType BuyType { get; init; }

    public static GachaBuyRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GachaBuyRequest { BuyType = (GachaBuyType)reader.ReadUInt() };
    }
}
