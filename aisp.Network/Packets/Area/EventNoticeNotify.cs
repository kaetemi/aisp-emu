using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// Server-to-client notice window (recv_event_notice / 0xCD6F).
/// Layout: name (nt, max 37), text (nt, max 1537), talkType (u32).
/// </summary>
public sealed class EventNoticeNotify : IOutgoingPacket
{
    public string Name { get; set; }
    public string Text { get; set; }
    public uint TalkType { get; set; }

    public EventNoticeNotify(string name, string text, uint talkType = 0)
    {
        Name = name;
        Text = text;
        TalkType = talkType;
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Name, 36);
        writer.Write(Text, 1536);
        // July 2009: two CStrings only. A trailing talkType uint left 4 bytes
        // unconsumed and VCE-reset Area (verified 2026-09-18, 60-byte dump).
        if (TalkType != 0)
            writer.Write(TalkType);
        return writer.ToBytes();
    }

    public static EventNoticeNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var name = reader.ReadString();
        var text = reader.ReadString();
        var consumed =
            PacketEncoding.GetEncoding("utf-8").GetByteCount(name)
            + 1
            + PacketEncoding.GetEncoding("utf-8").GetByteCount(text)
            + 1;
        var talkType = data.Length >= consumed + 4 ? reader.ReadUInt() : 0u;
        return new EventNoticeNotify(name, text, talkType);
    }
}
