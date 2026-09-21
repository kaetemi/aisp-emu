using aisp.Network.Data;

namespace aisp.Network.Packets.Msg;

public class AvatarGetCreateInfoResponse : IOutgoingPacket
{
    private readonly List<uint> DefaultMaleBuilds = [1001021, 1001011, 1001031];
    private readonly List<byte> DefaultMaleFaces = [0, 1, 2, 3];
    private readonly List<uint> DefaultMaleHairStyles = [10920010, 10920020, 10920040];
    private readonly List<byte> DefaultMaleHairColours = [0, 4, 1, 2, 3];
    private readonly List<ItemSlotInfo> DefaultMaleEquipment = [];

    private readonly List<uint> DefaultFemaleBuilds = [1002011, 1002021, 1002031];
    private readonly List<byte> DefaultFemaleFaces = [0, 1, 2, 3];
    private readonly List<uint> DefaultFemaleHairStyles = [10930010, 10930020, 10930040];
    private readonly List<byte> DefaultFemaleHairColours = [0, 4, 1, 2, 3];
    private readonly List<ItemSlotInfo> DefaultFemaleEquipment = [];

    public byte[] ToBytes()
    {
        DefaultMaleEquipment.Clear();
        DefaultMaleEquipment.Add(new ItemSlotInfo(10100220, 0)); //Shirt
        DefaultMaleEquipment.Add(new ItemSlotInfo(10200100, 0)); //Pants
        DefaultMaleEquipment.Add(new ItemSlotInfo(10400030, 0)); //Socks
        DefaultMaleEquipment.Add(new ItemSlotInfo(10500070, 0)); //Shoes

        DefaultFemaleEquipment.Clear();
        DefaultFemaleEquipment.Add(new ItemSlotInfo(10100060, 0)); //Shirt
        DefaultFemaleEquipment.Add(new ItemSlotInfo(10200090, 0)); //Shorts
        DefaultFemaleEquipment.Add(new ItemSlotInfo(10400000, 0)); //Socks
        DefaultFemaleEquipment.Add(new ItemSlotInfo(10500010, 0)); //Shoes

        var writer = new PacketWriter();
        // Male
        writer.Write((uint)DefaultMaleBuilds.Count);
        foreach (var v in DefaultMaleBuilds)
            writer.Write(v);
        writer.Write((uint)DefaultMaleFaces.Count);
        foreach (var v in DefaultMaleFaces)
            writer.Write(v);
        writer.Write((uint)DefaultMaleHairStyles.Count);
        foreach (var v in DefaultMaleHairStyles)
            writer.Write(v);
        writer.Write((uint)DefaultMaleHairColours.Count);
        foreach (var v in DefaultMaleHairColours)
            writer.Write(v);
        writer.Write((uint)DefaultMaleEquipment.Count);
        foreach (var eq in DefaultMaleEquipment)
            writer.Write(eq.ToBytes());
        // Female
        writer.Write((uint)DefaultFemaleBuilds.Count);
        foreach (var v in DefaultFemaleBuilds)
            writer.Write(v);
        writer.Write((uint)DefaultFemaleFaces.Count);
        foreach (var v in DefaultFemaleFaces)
            writer.Write(v);
        writer.Write((uint)DefaultFemaleHairStyles.Count);
        foreach (var v in DefaultFemaleHairStyles)
            writer.Write(v);
        writer.Write((uint)DefaultFemaleHairColours.Count);
        foreach (var v in DefaultFemaleHairColours)
            writer.Write(v);
        writer.Write((uint)DefaultFemaleEquipment.Count);
        foreach (var eq in DefaultFemaleEquipment)
            writer.Write(eq.ToBytes());

        return writer.ToBytes();
    }

    /// <summary>
    /// September 2008 <c>recv_get_avatar_create_info_r</c> (<c>0xA5AD</c>, parser <c>0x6bb39f</c>).
    /// Male then female, each: face bytes (max 4), hair-base uints (max 4), color-offset bytes (max 5),
    /// equipment pairs (max 29). Body models are hardcoded (1001011 / 1002011). The doll hair id is
    /// <c>base + color</c>, so 10920010 + 0..4 is one style's five colours.
    /// </summary>
    public byte[] ToSeptember2008Bytes()
    {
        byte[] faces = [0, 1, 2, 3];
        byte[] colors = [0, 1, 2, 3, 4];
        uint[] maleHair = [10920010, 10920020, 10920030, 10920040];
        uint[] femaleHair = [10930010, 10930020, 10930030, 10930040];
        ItemSlotInfo[] maleEquip =
        [
            new(10100140, 0),
            new(10100190, 0),
            new(10200130, 0),
            new(10400030, 0),
            new(10500070, 0),
        ];
        ItemSlotInfo[] femaleEquip =
        [
            new(10100060, 0),
            new(10200090, 0),
            new(10400000, 0),
            new(10500010, 0),
        ];

        var writer = new PacketWriter();
        WriteGender(writer, faces, maleHair, colors, maleEquip);
        WriteGender(writer, faces, femaleHair, colors, femaleEquip);
        return writer.ToBytes();
    }

    private static void WriteGender(
        PacketWriter writer,
        byte[] faces,
        uint[] hairBases,
        byte[] colorOffsets,
        ItemSlotInfo[] equipment
    )
    {
        writer.Write((uint)faces.Length);
        foreach (var face in faces)
            writer.Write(face);
        writer.Write((uint)hairBases.Length);
        foreach (var hair in hairBases)
            writer.Write(hair);
        writer.Write((uint)colorOffsets.Length);
        foreach (var color in colorOffsets)
            writer.Write(color);
        writer.Write((uint)equipment.Length);
        foreach (var equip in equipment)
            writer.Write(equip.ToBytes());
    }
}
