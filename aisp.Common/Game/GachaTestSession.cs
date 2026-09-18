namespace aisp.Common.Game;

/// <summary>
/// Last <c>/gacha</c> machine parameters so the crank handler can debit and pick a prize.
/// Visual ids are <c>./interface/package/%08d.dds</c> (str_table 100,950,20).
/// </summary>
internal static class GachaTestSession
{
    public const uint DefaultVisualId = 100000;
    public const uint DefaultPrizeItemId = 10100220;
    public const ulong DefaultAiPrice = 100;
    public const byte PrizeHitType = 1;

    /// <summary>Package splashes that exist in the July 2009 <c>interface.hed</c>.</summary>
    public static readonly (uint VisualId, string Label)[] Catalog =
    [
        (100000, "MoonScape"),
        (100001, "MoonScape 2"),
        (100003, "Halloween"),
        (100004, "Christmas"),
        (100005, "New Year 2009"),
        (100006, "Valentine"),
        (100007, "Sakura"),
        (100008, "Bear / sailor"),
        (100009, "Umbrella"),
        (100010, "Swim"),
        (101000, "DC店"),
        (102000, "CL店"),
        (103000, "SH店"),
        (200001, "Bellair furniture"),
    ];

    public static readonly uint[] PrizePool = [10100220, 10200100, 10400030, 10500070];

    public static uint VisualId { get; set; } = DefaultVisualId;
    public static uint PrizeItemId { get; set; } = DefaultPrizeItemId;
    public static ulong AiPrice { get; set; } = DefaultAiPrice;
    public static ulong NicoPrice { get; set; }

    public static uint PickVisual(uint? explicitId = null)
    {
        if (explicitId is > 0)
            return explicitId.Value;
        var pick = Catalog[Random.Shared.Next(Catalog.Length)].VisualId;
        if (Catalog.Length > 1 && pick == VisualId)
            pick = Catalog[Random.Shared.Next(Catalog.Length)].VisualId;
        return pick;
    }

    public static uint PickPrize() => PrizePool[Random.Shared.Next(PrizePool.Length)];
}
