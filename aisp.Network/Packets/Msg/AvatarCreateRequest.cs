using aisp.Network.Data;

namespace aisp.Network.Packets.Msg;

public class AvatarCreateRequest : IIncomingPacket<AvatarCreateRequest>
{
    public string AvatarName { get; set; } = string.Empty;
    public uint modelId;
    public CharaVisual visual = new(0, 0, 0, 0, 0, 0, 0);
    public uint slotId;

    public static AvatarCreateRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var name = reader.ReadString("utf-8");
        // September 2008 has no model id. The 19-byte visual's gender selects the
        // hardcoded body (1 -> 1001011, 2 -> 1002011). 2009/2011 send modelId first.
        uint modelId;
        CharaVisual visual;
        if (reader.Remaining == 23)
        {
            visual = CharaVisual.FromBytes(reader.ReadBytes(19));
            modelId = visual.Gender == 1 ? 1001011u : 1002011u;
        }
        else
        {
            modelId = reader.ReadUInt();
            visual = CharaVisual.FromBytes(reader.ReadBytes(19));
        }

        return new AvatarCreateRequest
        {
            AvatarName = name,
            modelId = modelId,
            visual = visual,
            slotId = reader.ReadUInt(),
        };
    }

    public override string ToString()
    {
        return $"[AvatarCreateRequest] Name: {AvatarName}, ModelId: {modelId}, SlotId: {slotId}, \r\n\tVisual: {visual}";
    }
}
