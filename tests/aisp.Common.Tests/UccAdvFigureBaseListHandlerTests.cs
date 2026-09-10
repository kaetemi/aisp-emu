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
    public async Task Empty_bag_returns_the_three_ip_figures_and_unowned_shop_definitions()
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
            Assert.Equal((uint)DramaFigures.AlwaysGranted.Count + 24, reader.ReadUInt());
            Assert.Equal(DramaFigures.DcBoxId, reader.ReadUInt());
            Assert.Equal(
                8 + (DramaFigures.AlwaysGranted.Count + 24) * UccAdvFigure.WireSize,
                sent.Payload.Length
            );
            AssertShopProbe(sent.Payload, owned: false);
            AssertPaidFigureVisuals(sent.Payload);
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
            Assert.Equal(27u, reader.ReadUInt());
            Assert.Equal(DramaFigures.DcBoxId, reader.ReadUInt());
            SkipFigureRest(ref reader);
            Assert.Equal(DramaFigures.ClannadBoxId, reader.ReadUInt());
            SkipFigureRest(ref reader);
            Assert.Equal(DramaFigures.ShuffleBoxId, reader.ReadUInt());
            SkipFigureRest(ref reader);
            Assert.Equal((DramaFigures.MenBoxId << 8) | 1u, reader.ReadUInt());
            Assert.Equal(DramaFigures.MenItemIdStart, reader.ReadUInt());
            AssertShopProbe(Assert.Single(session.Sent).Payload, owned: true);
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
            Assert.Equal(27u, reader.ReadUInt());
            AssertShopProbe(Assert.Single(session.Sent).Payload, owned: false);
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

    private static void AssertPaidFigureVisuals(ReadOnlySpan<byte> payload)
    {
        // Check the delivered registry, including later packages that previously lost
        // their heads when 1000..11000 was sent as the face variant.
        uint[] models = [1001021, 1001011, 1001031, 1002011, 1002021, 1002031];
        // Package artwork specifies both cut and color, including different wigs
        // for figures with the same face name at different heights.
        uint[] wigs =
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
        for (var index = 0; index < 24; index++)
        {
            var reader = new PacketReader(
                payload.Slice(8 + (3 + index) * UccAdvFigure.WireSize, UccAdvFigure.WireSize)
            );
            var box = index < 12 ? 1u : 2u;
            var package = (uint)(index % 12);
            Assert.Equal((box << 8) | (package + 1), reader.ReadUInt());
            Assert.Equal((index < 12 ? 14100000u : 14200000u) + package, reader.ReadUInt());
            reader.ReadBytes(UccAdvFigure.NameBytes);
            Assert.Equal(0, reader.ReadByte());
            Assert.Equal(index < 12 ? 1u : 2u, reader.ReadUInt()); // gender selects coverage rules
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt()); // default face, independent of package
            Assert.Equal(wigs[index], reader.ReadUInt()); // base hair survives equipment changes
            Assert.Equal(models[index / 4], reader.ReadUInt());
            Assert.Equal(box, reader.ReadUInt());
            Assert.Equal(package * 1000, reader.ReadUInt());
            uint[] clothing =
                box == 1
                    ? [10100220, 10200100, 10400030, 10500070, 10700030]
                    : [10100060, 10200090, 10400000, 10500010, 10600000, 10700000];
            foreach (var itemId in clothing)
                Assert.Equal(itemId, reader.ReadUInt());
            // Base hair must not also appear as a removable wardrobe item.
            for (var slot = clothing.Length; slot < UccAdvFigure.EquipSlotCount; slot++)
                Assert.Equal(0u, reader.ReadUInt());
        }
    }

    private static void AssertShopProbe(ReadOnlySpan<byte> payload, bool owned)
    {
        var reader = new PacketReader(payload[^(24 * UccAdvFigure.WireSize)..]);
        Assert.Equal(0x101u, reader.ReadUInt());
        Assert.Equal(DramaFigures.MenItemIdStart, reader.ReadUInt());
        reader.ReadBytes(UccAdvFigure.NameBytes);
        Assert.Equal(owned ? (byte)1 : (byte)0, reader.ReadByte());
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
