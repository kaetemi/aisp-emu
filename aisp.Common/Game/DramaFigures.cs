using aisp.Common.DAL.Entities;
using aisp.Network.Data;

namespace aisp.Common.Game;

/// <summary>
/// The figure boxes the drama notebook's picker offers a character. The three licensed boxes
/// (D.C.II, CLANNAD, SHUFFLE!) come with one doll each and belong to everyone; the ai sp@ce MEN
/// and WOMEN boxes hold twelve dolls each, one per bag item in the 141xxxxx and 142xxxxx range,
/// and a character gets the dolls whose items are in their bag.
/// </summary>
public static class DramaFigures
{
    public const uint MenBoxId = 1;
    public const uint WomenBoxId = 2;
    public const uint DcBoxId = 1000;
    public const uint ClannadBoxId = 1001;
    public const uint ShuffleBoxId = 1002;

    public const uint MenItemIdStart = 14_100_000;
    public const uint WomenItemIdStart = 14_200_000;

    /// <summary>A doll that comes with a bag item.</summary>
    public sealed record PaidFigure(uint ItemId, UccAdvFigure Figure);

    public static IReadOnlyList<NiconiCommonsEntry> Titles { get; } =
    [
        new() { Id = MenBoxId, Name = "ai sp@ce MEN" },
        new() { Id = WomenBoxId, Name = "ai sp@ce WOMEN" },
        new() { Id = DcBoxId, Name = "D.C.II" },
        new() { Id = ClannadBoxId, Name = "CLANNAD" },
        new() { Id = ShuffleBoxId, Name = "SHUFFLE!" },
    ];

    /// <summary>
    /// The licensed dolls, dressed in their gmchara.csv outfits, with their named wigs as
    /// base hair so the dressing curtain cannot leave them bald.
    /// </summary>
    public static IReadOnlyList<UccAdvFigure> AlwaysGranted { get; } =
    [
        Licensed(
            DcBoxId,
            "朝倉音姫モデル",
            2_012_011,
            10900000,
            [10100260, 10100000, 11500040, 10500050, 10600020, 10700020]
        ),
        Licensed(
            ClannadBoxId,
            "古河渚モデル",
            2_022_011,
            10900020,
            [10100010, 10200000, 10400000, 10500040, 10600000, 10700000]
        ),
        Licensed(
            ShuffleBoxId,
            "リシアンサスモデル",
            2_032_011,
            10900010,
            [10100020, 10200010, 10500030, 10400010, 10600000, 10700000, 10000000]
        ),
    ];

    public static IReadOnlyList<PaidFigure> Purchasable { get; } = BuildPurchasable();

    /// <summary>The licensed dolls, then the paid dolls whose bag items are among <paramref name="itemIds"/>.</summary>
    public static IReadOnlyList<UccAdvFigure> OwnedBy(IEnumerable<int> itemIds)
    {
        var owned = itemIds.Where(ItemEntityMapper.IsDramaFigureItem).ToHashSet();
        if (owned.Count == 0)
            return AlwaysGranted;
        return AlwaysGranted
            .Concat(
                Purchasable
                    .Where(paid => owned.Contains((int)paid.ItemId))
                    .Select(paid => paid.Figure)
            )
            .ToArray();
    }

    /// <summary>The dolls a character may use: the items in their bag decide the paid ones.</summary>
    public static IReadOnlyList<UccAdvFigure> OwnedBy(Character? character) =>
        OwnedBy(OwnedItemIds(character));

    /// <summary>The titles whose boxes hold at least one doll the character may use.</summary>
    public static IReadOnlyList<NiconiCommonsEntry> TitlesOwnedBy(IEnumerable<int> itemIds)
    {
        var boxes = OwnedBy(itemIds).Select(figure => figure.BoxId).ToHashSet();
        return Titles.Where(title => boxes.Contains(title.Id)).ToArray();
    }

    public static IReadOnlyList<NiconiCommonsEntry> TitlesOwnedBy(Character? character) =>
        TitlesOwnedBy(OwnedItemIds(character));

    private static IEnumerable<int> OwnedItemIds(Character? character) =>
        character is null
            ? []
            : character.Inventory.Where(stack => stack.Quantity > 0).Select(stack => stack.ItemId);

    private static UccAdvFigure Licensed(
        uint boxId,
        string name,
        uint modelId,
        uint wig,
        uint[] equipment
    ) =>
        new()
        {
            FigureId = boxId,
            BoxId = boxId,
            Gender = 2,
            Name = name,
            ModelId = modelId,
            Hairstyle = wig,
            Equipment = equipment,
        };

    // Twelve dolls per box: three heights by four faces, in the order of the bag items, with
    // the box art at package_{index * 1000}.dds (the archive has package_0 to package_11000
    // for boxes 1 and 2). The height picks the model; the base hairstyle matches the
    // package's wig. Face currently uses the default variant.
    private static List<PaidFigure> BuildPurchasable()
    {
        string[] menFaces = ["りりしい", "つり目な", "たれ目な", "クールな"];
        string[] womenFaces = ["かわいい", "つり目な", "たれ目な", "クールな"];
        string[] heights = ["長身", "中背", "小柄"];
        uint[] menModels = [1_001_021, 1_001_011, 1_001_031];
        uint[] womenModels = [1_002_011, 1_002_021, 1_002_031];
        // The client requires underwear coverage to open the dressing curtain and
        // accept the character: lower underwear for men, upper and lower for women.
        uint[] menEquip = [10100220, 10200100, 10400030, 10500070, 10700030];
        uint[] womenEquip = [10100060, 10200090, 10400000, 10500010, 10600000, 10700000];
        // Per-package wig styles and colors, in item order.
        uint[] menWigs =
        [
            10920010,
            10920024,
            10920041,
            10920012,
            10920023,
            10920040,
            10920014,
            10920031,
            10920042,
            10920013,
            10920030,
            10920044,
        ];
        uint[] womenWigs =
        [
            10930010,
            10930024,
            10930041,
            10930012,
            10930023,
            10930040,
            10930014,
            10930021,
            10930042,
            10930013,
            10930020,
            10930044,
        ];

        var figures = new List<PaidFigure>(24);
        AddPaid(
            figures,
            MenItemIdStart,
            MenBoxId,
            1,
            heights,
            menFaces,
            menModels,
            menEquip,
            menWigs
        );
        AddPaid(
            figures,
            WomenItemIdStart,
            WomenBoxId,
            2,
            heights,
            womenFaces,
            womenModels,
            womenEquip,
            womenWigs
        );
        return figures;
    }

    private static void AddPaid(
        List<PaidFigure> figures,
        uint itemIdStart,
        uint boxId,
        uint gender,
        string[] heights,
        string[] faces,
        uint[] models,
        uint[] equipment,
        uint[] wigs
    )
    {
        for (var height = 0; height < heights.Length; height++)
        {
            for (var face = 0; face < faces.Length; face++)
            {
                var index = (uint)(height * faces.Length + face);
                figures.Add(
                    new PaidFigure(
                        itemIdStart + index,
                        new UccAdvFigure
                        {
                            // A 16-bit id for the picker: the box in the high byte, the doll
                            // number in the low one.
                            FigureId = (boxId << 8) | (index + 1),
                            IconId = itemIdStart + index,
                            BoxId = boxId,
                            Gender = gender,
                            Name = $"{heights[height]}+{faces[face]}モデル",
                            PackageId = index * 1000,
                            ModelId = models[height],
                            Hairstyle = wigs[index],
                            Equipment = equipment,
                        }
                    )
                );
            }
        }
    }
}
