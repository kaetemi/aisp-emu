namespace aisp.Common.DAL.Entities;

/// <summary>
/// Stored ability-group index. Numeric values are the DB enum. Names
/// <see cref="ModifierType0"/> / <see cref="ModifierType1"/> / <see cref="ModifierType2"/>
/// are a TPS-era guess; the wire groups are unlabeled sequences of five uints
/// (2011 logs <c>ability</c> + <c>value=</c>).
/// </summary>
public enum RoboBattleAbilitySet : byte
{
    Base = 0,

    /// <summary>First extra group. On the July 2009 wire as <c>CharaBattleData.AbilityGroup0</c>.</summary>
    ModifierType0 = 1,

    /// <summary>Second extra group. On the July 2009 wire as <c>CharaBattleData.AbilityGroup1</c>.</summary>
    ModifierType1 = 2,

    /// <summary>Third extra group. 2011-only on the wire; stored here, omitted from the July 2009 blob.</summary>
    ModifierType2 = 3,
}

public sealed class RoboBattleAbility
{
    public int CharacterId { get; set; }
    public uint RoboId { get; set; }
    public RoboBattleAbilitySet AbilitySet { get; set; }
    public byte AbilityIndex { get; set; }
    public uint Value { get; set; }

    /// <summary>Parent battle row. Navigation name is historical; see <see cref="RoboTpsBattleData"/>.</summary>
    public RoboTpsBattleData TpsBattleData { get; set; } = default!;
}
