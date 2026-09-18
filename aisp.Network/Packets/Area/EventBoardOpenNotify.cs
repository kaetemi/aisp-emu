using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>recv_event_board_open</c> (0xFC57). Opens the in-game IE board window
/// (<c>CBrowserWindow</c> / 2011 <c>CBoardBrowserWindow</c>, PAS <c>bbs.xml</c> or <c>browser.xml</c>).
/// Layout: NUL-terminated UTF-8 URL, max 0x181 including the NUL.
/// </summary>
public sealed class EventBoardOpenNotify(string url) : IOutgoingPacket
{
    public const int UrlMaxBytesIncludingNul = 0x181;

    public string Url { get; } = url;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Url, UrlMaxBytesIncludingNul - 1);
        return writer.ToBytes();
    }

    public static EventBoardOpenNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EventBoardOpenNotify(reader.ReadString());
    }
}
