namespace aisp.Network.Packets.Area;

/// <summary><c>send_event_board_close</c> (0x4A90). Empty; the board chrome was closed locally.</summary>
public sealed class EventBoardCloseRequest : IIncomingPacket<EventBoardCloseRequest>
{
    public static EventBoardCloseRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
