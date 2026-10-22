using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Localisation;
using aisp.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Tests;

public sealed class GachaRepositoryTests
{
    [Fact]
    public async Task Seed_PlacesTheStationMachineWithItsPrizesAndNpc()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            await using var db = new MainContext(options);
            var seedDirectory = Path.Combine(AppContext.BaseDirectory, "seedData");
            await ItemRepository.SeedItemsIfEmptyAsync(
                db,
                Path.Combine(seedDirectory, "baseItems.json"),
                ct
            );
            var gachaPath = Path.Combine(seedDirectory, "gachaMachines.json");
            await GachaRepository.SeedFromJsonAsync(db, gachaPath, ct: ct);
            await GachaRepository.SeedFromJsonAsync(db, gachaPath, ct: ct);

            var machine = await db
                .GachaMachines.Include(x => x.Prizes)
                .Include(x => x.Npcs)
                .SingleAsync(x => x.Code == "akihabara_station_aipon", ct);
            Assert.Equal("aiぽん", machine.DisplayName);
            Assert.Equal(100000, machine.VisualId);
            Assert.Equal(100, machine.AiPrice);
            Assert.Equal(0, machine.NicoPrice);
            Assert.Null(machine.TicketItemId);
            Assert.True(machine.IsEnabled);
            Assert.Equal(10, machine.Prizes.Count);
            Assert.All(machine.Prizes, prize => Assert.True(prize.Weight > 0));
            var itemIds = machine.Prizes.Select(x => x.ItemId).ToList();
            Assert.Equal(itemIds.Count, await db.Items.CountAsync(x => itemIds.Contains(x.Id), ct));

            var npc = Assert.Single(machine.Npcs);
            Assert.Equal(10990100L, npc.MapId);
            Assert.Equal(1342177350L, npc.NpcObjectId);
            Assert.Equal(8800100L, npc.ModelId);
            Assert.Equal(NpcInteractionType.Gacha, npc.InteractionType);
            Assert.Equal(NpcEventKind.None, npc.EventKind);
            Assert.Null(npc.ShopId);
            Assert.Equal(1, await db.Npcs.CountAsync(x => x.GachaMachineId == machine.Id, ct));

            var rows = LocalisationCatalogSeeder.CollectFromDirectory(seedDirectory);
            Assert.Contains(
                rows,
                row =>
                    row.Key == L.Gacha.DisplayName("akihabara_station_aipon").Value
                    && row.Language == GameLanguage.Japanese
                    && row.Value == "aiぽん"
            );
            Assert.Contains(rows, row => row.Key == L.Npc.Name(1342177350).Value);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Seed_SkipsPrizesWhoseItemIsUnknown()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            var path = Path.Combine(Path.GetTempPath(), $"gacha-seed-{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(
                path,
                """
                {
                  "version": 1,
                  "machines": [
                    {
                      "code": "test",
                      "displayName": { "ja": "テスト" },
                      "visualId": 100003,
                      "aiPrice": 0,
                      "nicoPrice": 300,
                      "ticketItemId": 999,
                      "prizes": [
                        { "itemId": 10000060, "weight": 5, "quantity": 2, "hitType": 0 },
                        { "itemId": 999, "weight": 5 }
                      ]
                    }
                  ]
                }
                """,
                ct
            );
            try
            {
                await using var db = new MainContext(options);
                db.Items.Add(new Item { Id = 10000060, Name = "野球帽(黒)" });
                await db.SaveChangesAsync(ct);

                await GachaRepository.SeedFromJsonAsync(db, path, ct: ct);

                var machine = await db
                    .GachaMachines.Include(x => x.Prizes)
                    .SingleAsync(x => x.Code == "test", ct);
                Assert.Equal(300, machine.NicoPrice);
                Assert.Null(machine.TicketItemId);
                var prize = Assert.Single(machine.Prizes);
                Assert.Equal(10000060, prize.ItemId);
                Assert.Equal(2, prize.Quantity);
                Assert.Equal(0, prize.HitType);
            }
            finally
            {
                File.Delete(path);
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public void Draw_FollowsWeightsAndSkipsDisabledOrWeightlessPrizes()
    {
        var machine = new GachaMachine();
        machine.Prizes.Add(new GachaPrize { ItemId = 1, Weight = 3 });
        machine.Prizes.Add(new GachaPrize { ItemId = 2, Weight = 1 });
        machine.Prizes.Add(new GachaPrize { ItemId = 3, Weight = 0 });
        machine.Prizes.Add(
            new GachaPrize
            {
                ItemId = 4,
                Weight = 100,
                IsEnabled = false,
            }
        );

        var random = new Random(1234);
        var counts = new Dictionary<int, int>();
        for (var i = 0; i < 4000; i++)
        {
            var prize = GachaRepository.Draw(machine, random);
            Assert.NotNull(prize);
            counts[prize.ItemId] = counts.GetValueOrDefault(prize.ItemId) + 1;
        }

        Assert.DoesNotContain(3, counts.Keys);
        Assert.DoesNotContain(4, counts.Keys);
        Assert.InRange(counts[1], 2700, 3300);
        Assert.InRange(counts[2], 700, 1300);
        Assert.Null(GachaRepository.Draw(new GachaMachine(), random));
    }
}
