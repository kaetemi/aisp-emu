using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Game;

/// <summary>
/// Shop rows for ニコニ・コモンズショップ. Commons and voice ids are the client's own
/// registry ids for those entries.
/// Figure ids share the notebook registry; Purchasable maps them to bag items.
/// </summary>
public static class NiconiCommonsShopCatalog
{
    public const uint FigureAiPrice = 5000;
    public const uint CommonsAiPrice = 100;
    public const uint VoiceAiPrice = 500;
    public const uint NicoPrice = 0;

    // Shared artwork from item.hed.
    public const uint GirlVoiceIconId = 14_300_001;
    public const uint BgmIconId = 14_300_002;
    public const uint SeIconId = 14_300_003;

    public static uint IconIdFor(NiconiCommonsShopItemRecord row) =>
        row.Type switch
        {
            0 => PaidFigureForShopId(row.Id)?.ItemId
                ?? throw new ArgumentOutOfRangeException(nameof(row)),
            1 => GirlVoiceIconId,
            2 => BgmIconId,
            3 => SeIconId,
            _ => throw new ArgumentOutOfRangeException(nameof(row)),
        };

    /// <summary>Nine drama BGM tracks and one sound effect.</summary>
    public static IReadOnlyList<NiconiCommonsShopItemRecord> CommonsRows { get; } =
    [
        new(2, 32_001_001, CommonsAiPrice, NicoPrice),
        new(2, 32_001_002, CommonsAiPrice, NicoPrice),
        new(2, 32_001_003, CommonsAiPrice, NicoPrice),
        new(2, 32_001_004, CommonsAiPrice, NicoPrice),
        new(2, 32_001_005, CommonsAiPrice, NicoPrice),
        new(2, 32_001_006, CommonsAiPrice, NicoPrice),
        new(2, 32_001_007, CommonsAiPrice, NicoPrice),
        new(2, 32_001_008, CommonsAiPrice, NicoPrice),
        new(2, 32_001_009, CommonsAiPrice, NicoPrice),
        new(3, 42_001_001, CommonsAiPrice, NicoPrice),
    ];

    /// <summary>All twelve MEN and twelve WOMEN avatar definitions.</summary>
    public static IReadOnlyList<NiconiCommonsShopItemRecord> FigureRows { get; } =
        DramaFigures
            .Purchasable.Select(paid => new NiconiCommonsShopItemRecord(
                0,
                paid.Figure.FigureId,
                FigureAiPrice,
                NicoPrice
            ))
            .ToArray();

    /// <summary>少女ａ タイトルコール１／２ and 出会い, by their voice registry ids.</summary>
    public static IReadOnlyList<NiconiCommonsShopItemRecord> VoiceRows { get; } =
    [
        new(1, 10_001, VoiceAiPrice, NicoPrice),
        new(1, 10_002, VoiceAiPrice, NicoPrice),
        new(1, 10_003, VoiceAiPrice, NicoPrice),
    ];

    public static string CommonsName(uint id) =>
        id switch
        {
            32_001_001 => "[ai sp@ce]BGM01・朝（登校）",
            32_001_002 => "[ai sp@ce]BGM02・夜",
            32_001_003 => "[ai sp@ce]BGM03・怪しい、不審",
            32_001_004 => "[ai sp@ce]BGM04・ノスタルジー",
            32_001_005 => "[ai sp@ce]BGM05・クライマックス！",
            32_001_006 => "[ai sp@ce]BGM06・ジャズ",
            32_001_007 => "[ai sp@ce]BGM07・８bit",
            32_001_008 => "[ai sp@ce]BGM08・ケルト調",
            32_001_009 => "[ai sp@ce]BGM09・雅楽風",
            42_001_001 => "[ai sp@ce]SE01・ファンファーレ",
            _ => throw new ArgumentOutOfRangeException(nameof(id)),
        };

    // The client's names for these entries.
    public static string VoiceName(uint id) =>
        id switch
        {
            10_001 => "少女ａ・タイトルコール１",
            10_002 => "少女ａ・タイトルコール２",
            10_003 => "少女ａ・出会い",
            _ => throw new ArgumentOutOfRangeException(nameof(id)),
        };

    public static NiconiCommonsShopItemNotify Snapshot() => new(CommonsRows, FigureRows, VoiceRows);

    public static DramaFigures.PaidFigure? PaidFigureForShopId(uint figureId) =>
        DramaFigures.Purchasable.FirstOrDefault(paid => paid.Figure.FigureId == figureId);

    public static bool SellsFigure(uint id) => FigureRows.Any(row => row.Id == id);

    public static bool SellsCommons(uint id) => CommonsRows.Any(row => row.Id == id);

    public static bool SellsVoice(uint id) => VoiceRows.Any(row => row.Id == id);

    /// <summary>
    /// Bag id that remembers a bought BGM/SE row: the registry id offset into a range
    /// (172… and 182…) no seeded item or figure box (14_1xx_xxx / 14_2xx_xxx) occupies.
    /// </summary>
    public static int CommonsBagItemId(uint id) => checked((int)(140_000_000u + id));

    /// <summary>Bag id that remembers a bought charadoll voice, likewise offset to 143_01x_xxx.</summary>
    public static int VoiceBagItemId(uint id) => checked((int)(143_000_000u + id));
}
