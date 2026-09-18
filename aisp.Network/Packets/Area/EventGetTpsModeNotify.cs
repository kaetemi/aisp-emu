namespace aisp.Network.Packets.Area;

/// <summary>
/// 2011-only. Asks the client whether its TPS controller is active. The client
/// answers with <see cref="EventGetTpsModeRequest"/>. The July 2009 exe has no
/// <c>tps::</c> UI and no recv for opcode <c>0xD758</c>; do not send this on that
/// wire. Character hitpoint/stamina/tank/ability live on
/// <see cref="aisp.Network.Data.CharaBattleData"/>, not here.
/// </summary>
public sealed class EventGetTpsModeNotify : IOutgoingPacket
{
    public byte[] ToBytes() => [];
}
