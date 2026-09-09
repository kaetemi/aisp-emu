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
    public async Task Empty_bag_returns_the_three_ip_figures()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 7001, TestContext.Current.CancellationToken);
            await using var db = new MainContext(options);
            var session = new CapturingPlayerSession { CharacterId = 7001 };
            var handler = new AreaUccAdvFigureBaseListHandler(
                new CharacterRepository(db, NullLogger<CharacterRepository>.Instance)
            );

            await handler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            var sent = Assert.Single(session.Sent);
            Assert.Equal(PacketType.UccAdvFigureBaseListResponse, sent.Type);
            var reader = new PacketReader(sent.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal((uint)DramaFigures.AlwaysGranted.Count, reader.ReadUInt());
            Assert.Equal(DramaFigures.DcBoxId, reader.ReadUInt());
            Assert.Equal(
                8 + DramaFigures.AlwaysGranted.Count * UccAdvFigure.WireSize,
                sent.Payload.Length
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
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

            await using var verify = new MainContext(options);
            var session = new CapturingPlayerSession { CharacterId = 7002 };
            await new AreaUccAdvFigureBaseListHandler(
                new CharacterRepository(verify, NullLogger<CharacterRepository>.Instance)
            ).HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            var reader = new PacketReader(Assert.Single(session.Sent).Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(4u, reader.ReadUInt());
            Assert.Equal(DramaFigures.DcBoxId, reader.ReadUInt());
            SkipFigureRest(ref reader);
            Assert.Equal(DramaFigures.ClannadBoxId, reader.ReadUInt());
            SkipFigureRest(ref reader);
            Assert.Equal(DramaFigures.ShuffleBoxId, reader.ReadUInt());
            SkipFigureRest(ref reader);
            Assert.Equal((DramaFigures.MenBoxId << 8) | 1u, reader.ReadUInt());
            Assert.Equal(DramaFigures.MenBoxId, reader.ReadUInt());
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

            await using var verify = new MainContext(options);
            var session = new CapturingPlayerSession { CharacterId = 7003 };
            await new AreaUccAdvFigureBaseListHandler(
                new CharacterRepository(verify, NullLogger<CharacterRepository>.Instance)
            ).HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            var reader = new PacketReader(Assert.Single(session.Sent).Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(3u, reader.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public void OwnedBy_matches_purchasable_item_ids()
    {
        Assert.Equal(24, DramaFigures.Purchasable.Count);
        Assert.Equal(14100000u, DramaFigures.Purchasable[0].ItemId);
        Assert.True(DramaFigures.Purchasable[0].Figure.FigureId <= 0xFFFF);
        Assert.Equal(0u, DramaFigures.Purchasable[0].Figure.PackageId);
        Assert.Equal(14200011u, DramaFigures.Purchasable[^1].ItemId);
        Assert.Equal(11000u, DramaFigures.Purchasable[^1].Figure.PackageId);

        var owned = DramaFigures.OwnedBy([14100000, 14200004, 10100220]);
        Assert.Equal(5, owned.Count);
        Assert.Contains(owned, figure => figure.FigureId == ((DramaFigures.MenBoxId << 8) | 1u));
        Assert.Contains(owned, figure => figure.FigureId == ((DramaFigures.WomenBoxId << 8) | 5u));

        var emptyTitles = DramaFigures.TitlesOwnedBy([]);
        Assert.Equal(3, emptyTitles.Count);
        Assert.DoesNotContain(emptyTitles, title => title.Id == DramaFigures.MenBoxId);
        Assert.Contains(emptyTitles, title => title.Id == DramaFigures.DcBoxId);

        var menTitles = DramaFigures.TitlesOwnedBy([14100000]);
        Assert.Equal(4, menTitles.Count);
        Assert.Contains(menTitles, title => title.Id == DramaFigures.MenBoxId);
    }

    private static void SkipFigureRest(ref PacketReader reader)
    {
        reader.ReadUInt();
        reader.ReadBytes(UccAdvFigure.NameBytes);
        reader.ReadByte();
        for (var i = 0; i < UccAdvFigure.ExtraFieldCount + UccAdvFigure.EquipSlotCount * 2 + 1; i++)
            reader.ReadUInt();
    }
}
