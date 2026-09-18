namespace aisp.Network.Data;

public class CharaData(uint slotId, uint modelId, string name)
{
    /// <summary>July 2009 wire size. 20 bytes smaller than 2011 because <see cref="CharaBattleData"/> has three ability groups, not four.</summary>
    public const int WireSize = 546;
    public const int EquipmentSlotCount = 30;

    public uint SlotId { get; set; } = slotId;
    public uint ModelId { get; set; } = modelId;
    public string Name { get; set; } = name;
    public CharaVisual Visual = new(BloodType.A, 1, 1, 1, 2, 0, 0);
    public uint CharacterParameterId { get; set; }
    public CharacterMapData Map { get; set; } = new();

    /// <summary>Two floats after <see cref="Map"/>. No VCE field name. Previously labeled TpsActionReference; 2009 has no TPS action mgr.</summary>
    public float ActionReferenceX { get; set; }
    public float ActionReferenceY { get; set; }
    public uint ClientReserved { get; set; }

    /// <summary>Name plate variant (client CChara::SetNamePlate), a role the client's layout names: 0 none, 1 celebrity (pink), 2 GM (dark purple), 3 penalised user (red), 4 ordinary NPC (purple, the original's NPC plate), 5 official NPC (blue), 6 event user (orange with a yellow border), 0xFFFFFFFF creator/staff NPC (green); anything else draws no plate. Not a job id.</summary>
    public uint NamePlate { get; set; }

    /// <summary>Uint after name plate. No VCE field name. Previously labeled TpsActionProfileId.</summary>
    public uint ActionProfileId { get; set; }

    /// <summary>Unlabeled float. Collision meshes live under <c>tools/collision/</c>; this field is not proven to be a radius.</summary>
    public float CollisionRadius { get; set; }

    /// <summary>Float after <see cref="CollisionRadius"/>. No VCE field name. Previously labeled TpsActionVerticalRange.</summary>
    public float ActionVerticalRange { get; set; }
    public CharaBattleData Battle { get; set; } = new();
    public LevelProgressData Progress { get; set; } = new();

    public MovementData Movement
    {
        get => Map.Movement;
        set => Map.Movement = value;
    }

    public List<ItemSlotInfo> Equips = new(EquipmentSlotCount);

    public void AddEquip(uint id, uint socket)
    {
        Equips.Add(new ItemSlotInfo(id, socket));
    }

    public void AddEquip(
        IEnumerable<CharacterEquipSlot> equipment,
        Func<CharacterEquipSlot, uint> resolveSocket
    )
    {
        for (byte slot = 0; slot < EquipmentSlotCount; slot++)
        {
            var eq = equipment.FirstOrDefault(e => e.SlotIndex == slot);
            AddEquip(eq.ItemId, eq.ItemId != 0 ? resolveSocket(eq) : 0);
        }
    }

    public byte[] ToBytes()
    {
        while (Equips.Count < EquipmentSlotCount)
            AddEquip(0, 0);

        var writer = new PacketWriter();
        writer.Write(SlotId);
        writer.Write(ModelId);
        writer.WriteFixedString(Name, 37);
        writer.Write(Visual.ToBytes());
        writer.Write(CharacterParameterId);
        writer.Write(Map.ToBytes());
        writer.Write(ActionReferenceX);
        writer.Write(ActionReferenceY);
        for (var i = 0; i < EquipmentSlotCount; i++)
            writer.Write(Equips[i].ToBytes());
        writer.Write(ClientReserved);
        writer.Write(NamePlate);
        writer.Write(ActionProfileId);
        writer.Write(CollisionRadius);
        writer.Write(ActionVerticalRange);
        writer.Write(Battle.ToBytes());
        writer.Write(Progress.ToBytes());
        return writer.ToBytes();
    }

    public static CharaData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException(
                $"CharaData requires at least {WireSize} bytes.",
                nameof(data)
            );

        var reader = new PacketReader(data);
        var result = new CharaData(reader.ReadUInt(), reader.ReadUInt(), reader.ReadFixedString(37))
        {
            Visual = CharaVisual.FromBytes(reader.ReadBytes(19)),
            CharacterParameterId = reader.ReadUInt(),
            Map = CharacterMapData.FromBytes(reader.ReadBytes(CharacterMapData.WireSize)),
            ActionReferenceX = reader.ReadFloat(),
            ActionReferenceY = reader.ReadFloat(),
        };

        for (var i = 0; i < EquipmentSlotCount; i++)
            result.Equips.Add(new ItemSlotInfo(reader.ReadUInt(), reader.ReadUInt()));

        result.ClientReserved = reader.ReadUInt();
        result.NamePlate = reader.ReadUInt();
        result.ActionProfileId = reader.ReadUInt();
        result.CollisionRadius = reader.ReadFloat();
        result.ActionVerticalRange = reader.ReadFloat();
        result.Battle = CharaBattleData.FromBytes(reader.ReadBytes(CharaBattleData.WireSize));
        result.Progress = LevelProgressData.FromBytes(reader.ReadBytes(LevelProgressData.WireSize));
        return result;
    }
}
