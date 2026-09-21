using aisp.Network;

namespace aisp.Network.Packets.Area;

public class TimeZoneGetResponse(uint Result, uint Timezone, uint Time, uint TimeZoneMax, byte Flag)
    : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(Timezone);
        writer.Write(Time);
        writer.Write(TimeZoneMax);
        writer.Write(Flag);
        return writer.ToBytes();
    }

    /// <summary>
    /// September 2008 <c>0xCD38</c> reads four uints and exact-consumes them.
    /// The later flag byte fails that check and the area socket closes.
    /// </summary>
    public byte[] ToSeptember2008Bytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(Timezone);
        writer.Write(Time);
        writer.Write(TimeZoneMax);
        return writer.ToBytes();
    }
}
