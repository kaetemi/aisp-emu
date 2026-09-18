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

    public static uint VisualId { get; set; } = DefaultVisualId;
    public static uint PrizeItemId { get; set; } = DefaultPrizeItemId;
    public static ulong AiPrice { get; set; } = DefaultAiPrice;
    public static ulong NicoPrice { get; set; }
}
