using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>send_gacha_buy</c> (0x663A). One crank. BuyType 2 = P (ニコポ) when that price path wins; 0 or 1 = D (デレ).
/// </summary>
public sealed class GachaBuyRequest : IIncomingPacket<GachaBuyRequest>
{
    public uint BuyType { get; init; }

    public static GachaBuyRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GachaBuyRequest { BuyType = reader.ReadUInt() };
    }
}
