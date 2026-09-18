namespace aisp.Network.Data;

/// <summary>
/// Hit-point block. 2011 VCE logs <c>hitpoint=</c> / <c>hitpoint_max=</c> and
/// <c>heart=</c> / <c>heart_max=</c> on <c>recv_notify_update_hitpoint</c> /
/// <c>recv_notify_update_heart</c>. Same 18-byte layout in the July 2009
/// <c>CharaData</c> blob. Not TPS-specific; 2009 has no TPS UI.
/// </summary>
public sealed class HitPointData
{
    public const int WireSize = 18;

    public uint Current { get; set; }
    public uint BaseMaximum { get; set; }
    public uint MaximumBonus { get; set; }
    public uint MaximumPenalty { get; set; }
    public byte CurrentHearts { get; set; }
    public byte MaximumHearts { get; set; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Current);
        writer.Write(BaseMaximum);
        writer.Write(MaximumBonus);
        writer.Write(MaximumPenalty);
        writer.Write(CurrentHearts);
        writer.Write(MaximumHearts);
        return writer.ToBytes();
    }

    public static HitPointData FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new HitPointData
        {
            Current = reader.ReadUInt(),
            BaseMaximum = reader.ReadUInt(),
            MaximumBonus = reader.ReadUInt(),
            MaximumPenalty = reader.ReadUInt(),
            CurrentHearts = reader.ReadByte(),
            MaximumHearts = reader.ReadByte(),
        };
    }
}

/// <summary>
/// 16-byte gauge. 2011 <c>recv_notify_update_stamina</c> logs <c>gauge=</c> then
/// <c>speed=</c> for the two floats. <see cref="Speed"/> is that second float
/// (previously misnamed recovery-rate).
/// </summary>
public sealed class StaminaData
{
    public const int WireSize = 16;

    public float Current { get; set; }

    /// <summary>2011 VCE field <c>speed=</c>. Function in 2009 is unproven.</summary>
    public float Speed { get; set; }
    public uint CostReductionBonus { get; set; }
    public uint CostReductionPenalty { get; set; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Current);
        writer.Write(Speed);
        writer.Write(CostReductionBonus);
        writer.Write(CostReductionPenalty);
        return writer.ToBytes();
    }

    public static StaminaData FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new StaminaData
        {
            Current = reader.ReadFloat(),
            Speed = reader.ReadFloat(),
            CostReductionBonus = reader.ReadUInt(),
            CostReductionPenalty = reader.ReadUInt(),
        };
    }
}

/// <summary>
/// 16-byte gauge (four uints). 2011 protocol name is <c>tank</c>
/// (<c>recv_notify_update_tank</c> logs <c>amount=</c>; TPS HUD has
/// <c>CTankGauge</c>). 2009 has the same block and live tank recvs, without TPS UI.
/// </summary>
public sealed class TankData
{
    public const int WireSize = 16;

    public uint Current { get; set; }
    public uint BaseMaximum { get; set; }
    public uint MaximumBonus { get; set; }
    public uint MaximumPenalty { get; set; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Current);
        writer.Write(BaseMaximum);
        writer.Write(MaximumBonus);
        writer.Write(MaximumPenalty);
        return writer.ToBytes();
    }

    public static TankData FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new TankData
        {
            Current = reader.ReadUInt(),
            BaseMaximum = reader.ReadUInt(),
            MaximumBonus = reader.ReadUInt(),
            MaximumPenalty = reader.ReadUInt(),
        };
    }
}

/// <summary>
/// Five uints. 2011 <c>recv_notify_update_battle_ability</c> logs <c>ability</c> +
/// <c>value=</c> with an index 0–4. Not TPS-exclusive.
/// </summary>
public sealed class BattleAbilityValues
{
    public const int Count = 5;
    public const int WireSize = Count * sizeof(uint);

    public uint[] Values { get; set; } = new uint[Count];

    public byte[] ToBytes()
    {
        if (Values.Length != Count)
            throw new InvalidOperationException(
                $"BattleAbilityValues must contain exactly {Count} values."
            );

        var writer = new PacketWriter();
        foreach (var value in Values)
            writer.Write(value);
        return writer.ToBytes();
    }

    public static BattleAbilityValues FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var values = new uint[Count];
        for (var i = 0; i < values.Length; i++)
            values[i] = reader.ReadUInt();
        return new BattleAbilityValues { Values = values };
    }
}

public sealed class CosplayProgressData
{
    public const int WireSize = sizeof(uint) + LevelProgressData.WireSize;

    public uint CosplayId { get; set; }
    public LevelProgressData Progress { get; set; } = new();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(CosplayId);
        writer.Write(Progress.ToBytes());
        return writer.ToBytes();
    }

    public static CosplayProgressData FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CosplayProgressData
        {
            CosplayId = reader.ReadUInt(),
            Progress = LevelProgressData.FromBytes(reader.ReadBytes(LevelProgressData.WireSize)),
        };
    }
}

/// <summary>
/// Nested vitality / parameter / progression record on every <see cref="CharaData"/>
/// (avatars and robos). July 2009 wire size is 155 bytes (parser <c>0x719510</c>):
/// three ability groups of five uints. 2011 added a fourth group (+20 bytes) when TPS
/// landed; that group is <see cref="AbilityGroup2"/>, kept in memory and in the Robo DB
/// but omitted from this branch's wire.
///
/// This is not TPS state. TPS (2011-only <c>tps::</c> UI and
/// <c>recv_event_get_tps_mode</c> / <c>recv_notify_tps_use_item_*</c>) displays these
/// stats. The protocol names are <c>hitpoint</c>, <c>heart</c>, <c>stamina</c>
/// (<c>gauge</c>/<c>speed</c>), <c>tank</c>, <c>ability</c>, <c>cosplay</c>.
/// <c>recv_aipower_data</c> is a different 0x4C0-byte record, not this blob.
/// See <c>docs/CharacterBattleData.md</c>.
/// </summary>
public sealed class CharaBattleData
{
    public const int WireSize = 155;
    public const int WireAbilityGroupCount = 3;

    public HitPointData HitPoints { get; set; } = new();
    public StaminaData Stamina { get; set; } = new();
    public TankData Tank { get; set; } = new();
    public BattleAbilityValues BaseAbilities { get; set; } = new();
    public BattleAbilityValues AbilityGroup0 { get; set; } = new();
    public BattleAbilityValues AbilityGroup1 { get; set; } = new();

    /// <summary>2011-only fourth group. Not written or read on the July 2009 wire.</summary>
    public BattleAbilityValues AbilityGroup2 { get; set; } = new();
    public ulong StatusEffectFlags { get; set; }
    public uint ActionFlags { get; set; }
    public uint ActiveSkillId { get; set; }
    public CosplayProgressData Cosplay { get; set; } = new();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(HitPoints.ToBytes());
        writer.Write(Stamina.ToBytes());
        writer.Write(Tank.ToBytes());
        writer.Write(BaseAbilities.ToBytes());
        writer.Write(AbilityGroup0.ToBytes());
        writer.Write(AbilityGroup1.ToBytes());
        writer.Write(StatusEffectFlags);
        writer.Write(ActionFlags);
        writer.Write(ActiveSkillId);
        writer.Write(Cosplay.ToBytes());
        return writer.ToBytes();
    }

    public static CharaBattleData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException(
                $"CharaBattleData requires at least {WireSize} bytes.",
                nameof(data)
            );

        var reader = new PacketReader(data);
        return new CharaBattleData
        {
            HitPoints = HitPointData.FromBytes(reader.ReadBytes(HitPointData.WireSize)),
            Stamina = StaminaData.FromBytes(reader.ReadBytes(StaminaData.WireSize)),
            Tank = TankData.FromBytes(reader.ReadBytes(TankData.WireSize)),
            BaseAbilities = BattleAbilityValues.FromBytes(
                reader.ReadBytes(BattleAbilityValues.WireSize)
            ),
            AbilityGroup0 = BattleAbilityValues.FromBytes(
                reader.ReadBytes(BattleAbilityValues.WireSize)
            ),
            AbilityGroup1 = BattleAbilityValues.FromBytes(
                reader.ReadBytes(BattleAbilityValues.WireSize)
            ),
            StatusEffectFlags = reader.ReadULong(),
            ActionFlags = reader.ReadUInt(),
            ActiveSkillId = reader.ReadUInt(),
            Cosplay = CosplayProgressData.FromBytes(reader.ReadBytes(CosplayProgressData.WireSize)),
        };
    }
}
