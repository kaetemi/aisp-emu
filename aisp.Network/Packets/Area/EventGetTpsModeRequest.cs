namespace aisp.Network.Packets.Area;

/// <summary>
/// 2011-only client response to <c>recv_event_get_tps_mode</c>. Result is 1 while
/// TPS mode is active and 0 otherwise. Opcode <c>0xC290</c> is absent from the
/// July 2009 exe.
/// </summary>
public sealed class EventGetTpsModeRequest(uint result) : IIncomingPacket<EventGetTpsModeRequest>
{
    public uint Result { get; } = result;

    public static EventGetTpsModeRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EventGetTpsModeRequest(reader.ReadUInt());
    }
}
