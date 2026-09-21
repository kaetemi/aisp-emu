using aisp.Network;

namespace aisp.Network.Packets.Area;

public class AreasvEnterResponse(uint Result, uint ObjID) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(ObjID);
        return writer.ToBytes();
    }

    /// <summary>
    /// September 2008 parser <c>0x69813d</c> reads one uint and exact-consumes it.
    /// Result 0 advances the area scene from <c>0xFAA</c> to <c>0xFB4</c>.
    /// </summary>
    public byte[] ToSeptember2008Bytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
