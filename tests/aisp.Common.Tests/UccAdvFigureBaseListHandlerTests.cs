using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class UccAdvFigureBaseListHandlerTests
{
    [Fact]
    public async Task Empty_bag_returns_no_figures_for_the_july_2009_client()
    {
        var session = new CapturingPlayerSession { CharacterId = 7001 };
        var handler = new AreaUccAdvFigureBaseListHandler();

        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        var sent = Assert.Single(session.Sent);
        Assert.Equal(PacketType.UccAdvFigureBaseListResponse, sent.Type);
        var reader = new PacketReader(sent.Payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(8, sent.Payload.Length);
    }

    [Fact]
    public async Task Owned_men_box_is_appended_after_the_ip_set()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 7002, TestContext.Current.CancellationToken);
            await using (var db = new MainContext(options))
            {
                db.Items.Add(
                    new Item
                    {
                        Id = 14100000,
                        Name = "長身+りりしいモデル",
                        IconId = 14100000,
                    }
                );
                db.CharacterInventories.Add(
                    new CharacterInventory
                    {
                        CharacterId = 7002,
                        ItemId = 14100000,
                        Quantity = 1,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var session = new CapturingPlayerSession { CharacterId = 7002 };
            await new AreaUccAdvFigureBaseListHandler().HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            var reader = new PacketReader(Assert.Single(session.Sent).Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Unrelated_bag_items_do_not_grant_a_box()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 7003, TestContext.Current.CancellationToken);
            await using (var db = new MainContext(options))
            {
                db.Items.Add(
                    new Item
                    {
                        Id = 10100220,
                        Name = "シャツ",
                        IconId = 10100220,
                    }
                );
                db.CharacterInventories.Add(
                    new CharacterInventory
                    {
                        CharacterId = 7003,
                        ItemId = 10100220,
                        Quantity = 1,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var session = new CapturingPlayerSession { CharacterId = 7003 };
            await new AreaUccAdvFigureBaseListHandler().HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            var reader = new PacketReader(Assert.Single(session.Sent).Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Ownership_uses_catalog_item_mappings()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var lifetime = connection;
        await using var db = new MainContext(options);
        await DramaTestCatalog.SeedAsync(db);
        var catalog = new DramaCatalog(db, TestTextLocaliser.English);
        var character = new Character();
        character.Inventory.Add(new CharacterInventory { ItemId = 14100000, Quantity = 1 });
        character.Inventory.Add(new CharacterInventory { ItemId = 14200004, Quantity = 1 });
        character.Inventory.Add(new CharacterInventory { ItemId = 10100220, Quantity = 1 });
        var session = new CapturingPlayerSession();
        var figures = await catalog.FiguresAsync(
            character,
            session,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(27, figures.Count);
        Assert.Equal(5, figures.Count(x => x.Owned));
        Assert.Contains(figures, x => x.FigureId == 257 && x.Owned);
        Assert.Contains(figures, x => x.FigureId == 517 && x.Owned);
        var titles = await catalog.CommonsAsync(
            null,
            session,
            TestContext.Current.CancellationToken
        );
        Assert.DoesNotContain(titles, x => x.Id == 1);
        Assert.Contains(titles, x => x.Id == 1000);
    }
}
