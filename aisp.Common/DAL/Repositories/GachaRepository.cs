using System.Globalization;
using System.Text.Json;
using aisp.Common.DAL.Entities;
using aisp.Common.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace aisp.Common.DAL.Repositories;

public interface IGachaRepository
{
    /// <summary>An enabled machine with its enabled prizes, or null.</summary>
    Task<GachaMachine?> GetEnabledMachineAsync(int machineId, CancellationToken ct = default);
}

public sealed class GachaRepository(MainContext db) : IGachaRepository
{
    private static readonly JsonSerializerOptions JsonOptions = SeedJson.Options;

    public async Task<GachaMachine?> GetEnabledMachineAsync(
        int machineId,
        CancellationToken ct = default
    ) =>
        await db
            .GachaMachines.AsNoTracking()
            .Include(x =>
                x.Prizes.Where(p => p.IsEnabled).OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            )
            .FirstOrDefaultAsync(x => x.Id == machineId && x.IsEnabled, ct);

    /// <summary>
    /// Picks the capsule for one pull: each enabled prize with a positive weight wins with probability
    /// weight / total. Null when the machine has nothing to give.
    /// </summary>
    public static GachaPrize? Draw(GachaMachine machine, Random random)
    {
        var pool = machine
            .Prizes.Where(p => p.IsEnabled && p.Weight > 0 && p.Quantity > 0)
            .ToList();
        if (pool.Count == 0)
            return null;

        var total = pool.Sum(p => (long)p.Weight);
        var roll = random.NextInt64(total);
        foreach (var prize in pool)
        {
            roll -= prize.Weight;
            if (roll < 0)
                return prize;
        }

        return pool[^1];
    }

    public static async Task SeedFromJsonAsync(
        MainContext db,
        string jsonPath,
        ILogger? logger = null,
        CancellationToken ct = default
    )
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException("Gacha seed JSON not found.", jsonPath);

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var root =
            JsonSerializer.Deserialize<GachaSeedRoot>(json, JsonOptions) ?? new GachaSeedRoot();

        if (root.Version <= 0)
            throw new InvalidDataException("Gacha seed version must be greater than zero.");

        foreach (var machineRow in root.Machines)
        {
            if (string.IsNullOrWhiteSpace(machineRow.Code))
                throw new InvalidDataException("Gacha machine code is required.");
            if (machineRow.DisplayName.IsEmpty)
                throw new InvalidDataException(
                    $"Gacha machine {machineRow.Code} displayName is required."
                );
            if (machineRow.AiPrice < 0 || machineRow.NicoPrice < 0)
                throw new InvalidDataException(
                    $"Gacha machine {machineRow.Code} prices must not be negative."
                );
            if (
                machineRow.AiPrice == 0
                && machineRow.NicoPrice == 0
                && machineRow.TicketItemId is null
            )
                throw new InvalidDataException(
                    $"Gacha machine {machineRow.Code} needs an aiPrice, a nicoPrice or a ticketItemId."
                );
            if (machineRow.TicketsPerPull is <= 0)
                throw new InvalidDataException(
                    $"Gacha machine {machineRow.Code} ticketsPerPull must be greater than zero."
                );
            if (machineRow.RecycleItemsPerTicket is < 0)
                throw new InvalidDataException(
                    $"Gacha machine {machineRow.Code} recycleItemsPerTicket must not be negative."
                );
            if (machineRow.RecycleItemsPerTicket is > 0 && machineRow.TicketItemId is null)
                throw new InvalidDataException(
                    $"Gacha machine {machineRow.Code} recycleItemsPerTicket needs a ticketItemId to mint."
                );

            var machine = await db.GachaMachines.SingleOrDefaultAsync(
                x => x.Code == machineRow.Code,
                ct
            );
            if (machine is null)
            {
                machine = new GachaMachine { Code = machineRow.Code };
                db.GachaMachines.Add(machine);
            }

            machine.DisplayName = machineRow.DisplayName.Canonical;
            machine.VisualId = machineRow.VisualId;
            machine.AiPrice = machineRow.AiPrice;
            machine.NicoPrice = machineRow.NicoPrice;
            machine.TicketItemId = machineRow.TicketItemId;
            machine.TicketsPerPull = machineRow.TicketsPerPull ?? 1;
            machine.RecycleItemsPerTicket = machineRow.RecycleItemsPerTicket ?? 0;
            machine.IsEnabled = machineRow.IsEnabled ?? true;

            if (
                machineRow.TicketItemId is int ticketItemId
                && !await db.Items.AsNoTracking().AnyAsync(x => x.Id == ticketItemId, ct)
            )
            {
                logger?.LogWarning(
                    "Gacha machine {Code} ticket item {ItemId} does not exist in Items table; ticket pulls are disabled.",
                    machineRow.Code,
                    ticketItemId
                );
                machine.TicketItemId = null;
                machine.RecycleItemsPerTicket = 0;
            }

            await db.SaveChangesAsync(ct);

            var seenPrizeItems = new HashSet<int>();
            foreach (var prizeRow in machineRow.Prizes)
            {
                if (prizeRow.ItemId <= 0)
                    throw new InvalidDataException(
                        $"Gacha machine {machineRow.Code} has invalid prize item id {prizeRow.ItemId}."
                    );
                if (!seenPrizeItems.Add(prizeRow.ItemId))
                    throw new InvalidDataException(
                        $"Gacha machine {machineRow.Code} lists prize item {prizeRow.ItemId} twice."
                    );
                if (prizeRow.Weight is < 0)
                    throw new InvalidDataException(
                        $"Gacha machine {machineRow.Code} prize {prizeRow.ItemId} weight must not be negative."
                    );
                if (prizeRow.Quantity is <= 0 or > ushort.MaxValue)
                    throw new InvalidDataException(
                        $"Gacha machine {machineRow.Code} prize {prizeRow.ItemId} quantity must be 1..{ushort.MaxValue}."
                    );
                if (prizeRow.HitType is < 0 or > byte.MaxValue)
                    throw new InvalidDataException(
                        $"Gacha machine {machineRow.Code} prize {prizeRow.ItemId} hitType must be 0..{byte.MaxValue}."
                    );

                var itemExists = await db
                    .Items.AsNoTracking()
                    .AnyAsync(x => x.Id == prizeRow.ItemId, ct);
                if (!itemExists)
                {
                    logger?.LogWarning(
                        "Skipping gacha prize {ItemId} for machine {Code}; item does not exist in Items table.",
                        prizeRow.ItemId,
                        machineRow.Code
                    );
                    continue;
                }

                var prize = await db.GachaPrizes.SingleOrDefaultAsync(
                    x => x.GachaMachineId == machine.Id && x.ItemId == prizeRow.ItemId,
                    ct
                );
                if (prize is null)
                {
                    prize = new GachaPrize
                    {
                        GachaMachineId = machine.Id,
                        ItemId = prizeRow.ItemId,
                    };
                    db.GachaPrizes.Add(prize);
                }

                prize.Quantity = prizeRow.Quantity ?? 1;
                prize.Weight = prizeRow.Weight ?? 1;
                prize.HitType = prizeRow.HitType ?? 1;
                prize.SortOrder = prizeRow.SortOrder ?? 0;
                prize.IsEnabled = prizeRow.IsEnabled ?? true;
            }

            foreach (var npcRow in machineRow.Npcs)
            {
                if (npcRow.DayPhase is < -1 or > 4)
                    throw new InvalidDataException(
                        $"NPC {npcRow.NpcObjectId} dayPhase must be -1 or 0..4."
                    );
                if (npcRow.Name.IsEmpty)
                    throw new InvalidDataException($"NPC {npcRow.NpcObjectId} name is required.");

                var dateStartUtc = ParseUtc(
                    npcRow.DateStartUtc,
                    DateTime.UnixEpoch,
                    "dateStartUtc"
                );
                var dateEndUtc = ParseUtc(npcRow.DateEndUtc, DateTime.MaxValue, "dateEndUtc");
                if (dateStartUtc > dateEndUtc)
                    throw new InvalidDataException(
                        $"NPC {npcRow.NpcObjectId} dateStartUtc must be <= dateEndUtc."
                    );

                var npc = await db
                    .Npcs.Include(x => x.Equipment)
                    .SingleOrDefaultAsync(x => x.NpcObjectId == npcRow.NpcObjectId, ct);
                if (npc is null)
                {
                    npc = new Npc { NpcObjectId = npcRow.NpcObjectId };
                    db.Npcs.Add(npc);
                }

                npc.MapId = npcRow.MapId;
                npc.ChannelId = npcRow.ChannelId ?? -1;
                npc.DayPhase = npcRow.DayPhase ?? -1;
                npc.DateStartUtc = dateStartUtc;
                npc.DateEndUtc = dateEndUtc;
                npc.ModelId = npcRow.ModelId;
                npc.NamePlate = npcRow.NamePlate ?? Npc.DefaultNamePlate;
                npc.Name = npcRow.Name.Canonical;
                npc.X = npcRow.X;
                npc.Y = npcRow.Y;
                npc.Z = npcRow.Z;
                npc.Rotation = npcRow.Rotation;
                npc.ShopId = null;
                npc.GachaMachineId = machine.Id;
                npc.InteractionType = NpcInteractionType.Gacha;
                npc.EventKind = NpcEventKind.None;
                npc.EventKey = null;
                npc.IsEnabled = npcRow.IsEnabled ?? true;
                npc.SortOrder = npcRow.SortOrder ?? 0;
                if (npc.Equipment.Count > 0)
                    db.NpcEquipments.RemoveRange(npc.Equipment);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static DateTime ParseUtc(string? value, DateTime fallback, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        if (
            !DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed
            )
        )
            throw new InvalidDataException($"Invalid UTC timestamp for {fieldName}: '{value}'.");
        return parsed.UtcDateTime;
    }

    private sealed class GachaSeedRoot
    {
        public int Version { get; set; } = 1;
        public List<GachaMachineSeedRow> Machines { get; set; } = [];
    }

    private sealed class GachaMachineSeedRow
    {
        public string Code { get; set; } = string.Empty;
        public LocalisedString DisplayName { get; set; } = new();
        public long VisualId { get; set; }
        public long AiPrice { get; set; }
        public long NicoPrice { get; set; }
        public int? TicketItemId { get; set; }
        public int? TicketsPerPull { get; set; }
        public int? RecycleItemsPerTicket { get; set; }
        public bool? IsEnabled { get; set; }
        public List<GachaPrizeSeedRow> Prizes { get; set; } = [];
        public List<GachaNpcSeedRow> Npcs { get; set; } = [];
    }

    private sealed class GachaPrizeSeedRow
    {
        public int ItemId { get; set; }
        public int? Quantity { get; set; }
        public int? Weight { get; set; }
        public int? HitType { get; set; }
        public int? SortOrder { get; set; }
        public bool? IsEnabled { get; set; }
    }

    private sealed class GachaNpcSeedRow
    {
        public long MapId { get; set; }
        public int? ChannelId { get; set; }
        public int? DayPhase { get; set; }
        public string? DateStartUtc { get; set; }
        public string? DateEndUtc { get; set; }
        public long NpcObjectId { get; set; }
        public long ModelId { get; set; }
        public uint? NamePlate { get; set; }
        public LocalisedString Name { get; set; } = new();
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int Rotation { get; set; }
        public bool? IsEnabled { get; set; }
        public int? SortOrder { get; set; }
    }
}
