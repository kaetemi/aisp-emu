using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class FriendLinkTagGetResponse(
    uint result,
    uint avatarId,
    IReadOnlyList<FriendLinkTagData>? tagData = null,
    IReadOnlyList<uint>? slots = null,
    IReadOnlyList<FriendLinkTagData>? questionnaireTagData = null,
    IReadOnlyList<uint>? questionnaireSlots = null
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        PacketWriter writer = new();
        writer.Write(result);
        writer.Write(avatarId);
        WriteTags(writer, tagData);
        WriteSlots(writer, slots);
        WriteTags(writer, questionnaireTagData);
        WriteSlots(writer, questionnaireSlots);
        return writer.ToBytes();
    }

    /// <summary>
    /// September 2008 parser <c>0x69b173</c> reads result, avatar id, then two
    /// lists (tag records, then slot uints), each count at most 5, and
    /// exact-consumes. A tag record is a uint plus 61 name bytes. Result 0
    /// while the area scene is <c>0xFD2</c> advances it to <c>0xFDC</c>.
    /// The questionnaire lists are not on this exe.
    /// </summary>
    public byte[] ToSeptember2008Bytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(avatarId);
        WriteTags(writer, tagData);
        WriteSlots(writer, slots);
        return writer.ToBytes();
    }

    private static void WriteTags(PacketWriter writer, IReadOnlyList<FriendLinkTagData>? tags)
    {
        var count = Math.Min(tags?.Count ?? 0, 5);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            tags![i].Write(writer);
    }

    private static void WriteSlots(PacketWriter writer, IReadOnlyList<uint>? slots)
    {
        var count = Math.Min(slots?.Count ?? 0, 5);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            writer.Write(slots![i]);
    }
}
