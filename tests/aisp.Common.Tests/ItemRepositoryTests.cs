using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Tests;

public sealed class ItemRepositoryTests
{
    [Fact]
    public async Task EnsureSeedItemsPresentAsync_rewrites_stale_furniture_category_for_114_backpacks()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        var seedPath = Path.Combine(Path.GetTempPath(), $"aisp-items-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(
                seedPath,
                """
                [
                  {
                    "id": 11400020,
                    "socket": 26,
                    "name": { "ja": "スクールリュック" },
                    "iconId": 11400020
                  }
                ]
                """,
                TestContext.Current.CancellationToken
            );

            await using (var db = new MainContext(options))
            {
                db.Items.Add(
                    new Item
                    {
                        Id = 11400020,
                        Name = "スクールリュック",
                        Socket = 26,
                        IconId = 11400020,
                        CatalogCategory = (int)WardrobeCategoryId.FurnitureFloor,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                await ItemRepository.EnsureSeedItemsPresentAsync(
                    db,
                    seedPath,
                    TestContext.Current.CancellationToken
                );
            }

            await using (var db = new MainContext(options))
            {
                var item = await db.Items.SingleAsync(
                    x => x.Id == 11400020,
                    TestContext.Current.CancellationToken
                );
                Assert.Equal((int)WardrobeCategoryId.Accessory, item.CatalogCategory);
            }
        }
        finally
        {
            await connection.DisposeAsync();
            if (File.Exists(seedPath))
                File.Delete(seedPath);
        }
    }

    [Fact]
    public async Task EnsureSeedItemsPresentAsync_adds_missing_drama_figure_boxes()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        var seedPath = Path.Combine(Path.GetTempPath(), $"aisp-items-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(
                seedPath,
                """
                [
                  {
                    "id": 14100000,
                    "socket": 0,
                    "name": { "ja": "長身+りりしいモデル" },
                    "iconId": 14100000
                  }
                ]
                """,
                TestContext.Current.CancellationToken
            );

            await using (var db = new MainContext(options))
            {
                await ItemRepository.EnsureSeedItemsPresentAsync(
                    db,
                    seedPath,
                    TestContext.Current.CancellationToken
                );
            }

            await using (var db = new MainContext(options))
            {
                var item = await db.Items.SingleAsync(
                    x => x.Id == 14100000,
                    TestContext.Current.CancellationToken
                );
                Assert.Equal("長身+りりしいモデル", item.Name);
                Assert.Equal((int)WardrobeCategoryId.DramaFigure, item.CatalogCategory);
            }
        }
        finally
        {
            await connection.DisposeAsync();
            if (File.Exists(seedPath))
                File.Delete(seedPath);
        }
    }

    [Fact]
    public async Task EnsureSeedItemsPresentAsync_rewrites_stale_furniture_category_for_141_figure_boxes()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        var seedPath = Path.Combine(Path.GetTempPath(), $"aisp-items-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(
                seedPath,
                """
                [
                  {
                    "id": 14100000,
                    "socket": 0,
                    "name": { "ja": "長身+りりしいモデル" },
                    "iconId": 14100000
                  }
                ]
                """,
                TestContext.Current.CancellationToken
            );

            await using (var db = new MainContext(options))
            {
                db.Items.Add(
                    new Item
                    {
                        Id = 14100000,
                        Name = "長身+りりしいモデル",
                        Socket = 0,
                        IconId = 14100000,
                        CatalogCategory = (int)WardrobeCategoryId.FurnitureFloor,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                await ItemRepository.EnsureSeedItemsPresentAsync(
                    db,
                    seedPath,
                    TestContext.Current.CancellationToken
                );
            }

            await using (var db = new MainContext(options))
            {
                var item = await db.Items.SingleAsync(
                    x => x.Id == 14100000,
                    TestContext.Current.CancellationToken
                );
                Assert.Equal((int)WardrobeCategoryId.DramaFigure, item.CatalogCategory);
            }
        }
        finally
        {
            await connection.DisposeAsync();
            if (File.Exists(seedPath))
                File.Delete(seedPath);
        }
    }
}
