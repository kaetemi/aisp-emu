using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class AreaGachaHandlersTests
{
    private const uint StationMapId = 10990100;
    private const uint MachineNpcObjectId = 0x50000042;
    private const int PrizeItemId = 10000060;
    private const int TicketItemId = 10400000;

    [Fact]
    public async Task EventAccessNpc_OpensMachineAndSendsStarted()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            int machineId;
            await using (var db = new MainContext(options))
                machineId = await SeedMachineAsync(
                    db,
                    aiPrice: 100,
                    nicoPrice: 0,
                    ticketItemId: TicketItemId,
                    ticketsPerPull: 2
                );

            await using var runDb = new MainContext(options);
            var handler = CreateEventAccessNpcHandler(runDb);
            var session = new CapturingPlayerSession
            {
                MapId = StationMapId,
                CharacterId = 9001,
                ActiveShopId = 42,
            };

            await handler.HandleAsync(BuildEventAccessNpcPayload(MachineNpcObjectId), session, ct);

            Assert.Null(session.ActiveShopId);
            Assert.Equal(machineId, session.ActiveGachaMachineId);
            var response = Assert.Single(
                session.Sent,
                p => p.Type == PacketType.EventAccessNpcResponse
            );
            Assert.Equal(0u, new PacketReader(response.Payload).ReadUInt());
            var supply = Assert.Single(session.Sent, p => p.Type == PacketType.NotifySupplyNpcExec);
            Assert.Equal(MachineNpcObjectId, new PacketReader(supply.Payload).ReadUInt());
            var started = GachaStartedNotify.FromBytes(
                Assert.Single(session.Sent, p => p.Type == PacketType.GachaStartedNotify).Payload
            );
            Assert.Equal("aiPon", started.Name);
            Assert.Equal(100000u, started.VisualId);
            Assert.Equal(100ul, started.AiPrice);
            Assert.Equal(0ul, started.NicoPrice);
            Assert.Equal((uint)TicketItemId, started.TicketItemId);
            Assert.Equal((ushort)2, started.TicketsPerPull);
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.EventStartNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventAccessNpc_RejectsDisabledMachine()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            await using (var db = new MainContext(options))
                await SeedMachineAsync(db, aiPrice: 100, nicoPrice: 0, isEnabled: false);

            await using var runDb = new MainContext(options);
            var handler = CreateEventAccessNpcHandler(runDb);
            var session = new CapturingPlayerSession { MapId = StationMapId, CharacterId = 9001 };

            await handler.HandleAsync(BuildEventAccessNpcPayload(MachineNpcObjectId), session, ct);

            Assert.Null(session.ActiveGachaMachineId);
            var response = Assert.Single(
                session.Sent,
                p => p.Type == PacketType.EventAccessNpcResponse
            );
            Assert.Equal(1u, new PacketReader(response.Payload).ReadUInt());
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.GachaStartedNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Buy_WithAiPoints_DebitsPurseGrantsPrizeAndSyncsBag()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            var user = CreateUserWithCharacter(1, 9001, aiPoints: 250, nicoPoints: 999);
            int machineId;
            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                machineId = await SeedMachineAsync(db, aiPrice: 100, nicoPrice: 0);
            }

            await using var runDb = new MainContext(options);
            var session = CreateSession(user, machineId);
            var handler = CreateBuyHandler(runDb);

            await handler.HandleAsync(BuildBuyPayload(GachaBuyType.AiPoints), session, ct);

            var buy = GachaBuyResponse.FromBytes(
                Assert.Single(session.Sent, p => p.Type == PacketType.GachaBuyResponse).Payload
            );
            Assert.Equal(0u, buy.Result);
            Assert.Equal((uint)PrizeItemId, buy.SerialId);
            Assert.Equal((ushort)1, buy.Num);
            Assert.Equal((byte)1, buy.HitType);
            Assert.Equal(
                [
                    PacketType.GachaBuyResponse,
                    PacketType.MoneyUpdatedAipoint,
                    PacketType.ItemCreateNotify,
                ],
                session.Sent.Select(p => p.Type).ToArray()
            );
            Assert.Equal(150, session.User!.AiPoints);

            await using var verify = new MainContext(options);
            Assert.Equal(150, (await verify.Users.SingleAsync(u => u.Id == 1, ct)).AiPoints);
            Assert.Equal(999, (await verify.Users.SingleAsync(u => u.Id == 1, ct)).NicoPoints);
            var stack = await verify.CharacterInventories.SingleAsync(
                i => i.CharacterId == 9001 && i.ItemId == PrizeItemId,
                ct
            );
            Assert.Equal(1, stack.Quantity);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Buy_WithNicoPoints_DebitsNicoPurseOnly()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            var user = CreateUserWithCharacter(2, 9002, aiPoints: 50, nicoPoints: 800);
            int machineId;
            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                machineId = await SeedMachineAsync(db, aiPrice: 0, nicoPrice: 500);
            }

            await using var runDb = new MainContext(options);
            var session = CreateSession(user, machineId);
            await CreateBuyHandler(runDb)
                .HandleAsync(BuildBuyPayload(GachaBuyType.NicoPoints), session, ct);

            var buy = GachaBuyResponse.FromBytes(
                Assert.Single(session.Sent, p => p.Type == PacketType.GachaBuyResponse).Payload
            );
            Assert.Equal(0u, buy.Result);
            Assert.Contains(session.Sent, p => p.Type == PacketType.MoneyUpdatedNicopoint);
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.MoneyUpdatedAipoint);
            Assert.Equal(300, session.User!.NicoPoints);
            Assert.Equal(50, session.User.AiPoints);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Buy_WithTicket_ConsumesTicketItems()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            var user = CreateUserWithCharacter(3, 9003, aiPoints: 0, nicoPoints: 0);
            int machineId;
            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                machineId = await SeedMachineAsync(
                    db,
                    aiPrice: 100,
                    nicoPrice: 0,
                    ticketItemId: TicketItemId,
                    ticketsPerPull: 2
                );
                db.CharacterInventories.Add(
                    new CharacterInventory
                    {
                        CharacterId = 9003,
                        ItemId = TicketItemId,
                        Quantity = 5,
                    }
                );
                await db.SaveChangesAsync(ct);
            }

            await using var runDb = new MainContext(options);
            var session = CreateSession(user, machineId);
            await CreateBuyHandler(runDb)
                .HandleAsync(BuildBuyPayload(GachaBuyType.Ticket), session, ct);

            var buy = GachaBuyResponse.FromBytes(
                Assert.Single(session.Sent, p => p.Type == PacketType.GachaBuyResponse).Payload
            );
            Assert.Equal(0u, buy.Result);
            Assert.Equal(
                [
                    PacketType.GachaBuyResponse,
                    PacketType.ItemUpdateNumNotify,
                    PacketType.ItemCreateNotify,
                ],
                session.Sent.Select(p => p.Type).ToArray()
            );
            Assert.Equal(0, session.User!.AiPoints);

            await using var verify = new MainContext(options);
            var tickets = await verify.CharacterInventories.SingleAsync(
                i => i.CharacterId == 9003 && i.ItemId == TicketItemId,
                ct
            );
            Assert.Equal(3, tickets.Quantity);
            Assert.True(
                await verify.CharacterInventories.AnyAsync(
                    i => i.CharacterId == 9003 && i.ItemId == PrizeItemId && i.Quantity == 1,
                    ct
                )
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Buy_TicketOnlyMachine_TreatsKindZeroAsTicket()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            var user = CreateUserWithCharacter(8, 9008, aiPoints: 1000, nicoPoints: 0);
            int machineId;
            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                machineId = await SeedMachineAsync(
                    db,
                    aiPrice: 0,
                    nicoPrice: 0,
                    ticketItemId: TicketItemId,
                    ticketsPerPull: 1
                );
                db.CharacterInventories.Add(
                    new CharacterInventory
                    {
                        CharacterId = 9008,
                        ItemId = TicketItemId,
                        Quantity = 1,
                    }
                );
                await db.SaveChangesAsync(ct);
            }

            await using var runDb = new MainContext(options);
            var session = CreateSession(user, machineId);
            await CreateBuyHandler(runDb)
                .HandleAsync(BuildBuyPayload(GachaBuyType.AiPoints), session, ct);

            var buy = GachaBuyResponse.FromBytes(
                Assert.Single(session.Sent, p => p.Type == PacketType.GachaBuyResponse).Payload
            );
            Assert.Equal(0u, buy.Result);
            Assert.Equal(
                [
                    PacketType.GachaBuyResponse,
                    PacketType.ItemDeleteNotify,
                    PacketType.ItemCreateNotify,
                ],
                session.Sent.Select(p => p.Type).ToArray()
            );
            Assert.Equal(1000, session.User!.AiPoints);

            await using var verify = new MainContext(options);
            Assert.False(
                await verify.CharacterInventories.AnyAsync(
                    i => i.CharacterId == 9008 && i.ItemId == TicketItemId,
                    ct
                )
            );
            Assert.Equal(1000, (await verify.Users.SingleAsync(u => u.Id == 8, ct)).AiPoints);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Buy_PricedMachineWithTicket_KeepsKindZeroAsAiPoints()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            var user = CreateUserWithCharacter(9, 9009, aiPoints: 1000, nicoPoints: 0);
            int machineId;
            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                machineId = await SeedMachineAsync(
                    db,
                    aiPrice: 100,
                    nicoPrice: 0,
                    ticketItemId: TicketItemId,
                    ticketsPerPull: 1
                );
                db.CharacterInventories.Add(
                    new CharacterInventory
                    {
                        CharacterId = 9009,
                        ItemId = TicketItemId,
                        Quantity = 1,
                    }
                );
                await db.SaveChangesAsync(ct);
            }

            await using var runDb = new MainContext(options);
            var session = CreateSession(user, machineId);
            await CreateBuyHandler(runDb)
                .HandleAsync(BuildBuyPayload(GachaBuyType.AiPoints), session, ct);

            Assert.Equal(0u, GachaBuyResponse.FromBytes(session.Sent[0].Payload).Result);
            Assert.Contains(session.Sent, p => p.Type == PacketType.MoneyUpdatedAipoint);
            Assert.Equal(900, session.User!.AiPoints);

            await using var verify = new MainContext(options);
            var tickets = await verify.CharacterInventories.SingleAsync(
                i => i.CharacterId == 9009 && i.ItemId == TicketItemId,
                ct
            );
            Assert.Equal(1, tickets.Quantity);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Buy_RefusesWhenPurseIsShort()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            var user = CreateUserWithCharacter(4, 9004, aiPoints: 99, nicoPoints: 0);
            int machineId;
            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                machineId = await SeedMachineAsync(db, aiPrice: 100, nicoPrice: 0);
            }

            await using var runDb = new MainContext(options);
            var session = CreateSession(user, machineId);
            await CreateBuyHandler(runDb)
                .HandleAsync(BuildBuyPayload(GachaBuyType.AiPoints), session, ct);

            var buy = GachaBuyResponse.FromBytes(Assert.Single(session.Sent).Payload);
            Assert.Equal(AreaGachaBuyHandler.RefusedResult, buy.Result);
            Assert.Equal(0u, buy.SerialId);
            Assert.Equal(99, session.User!.AiPoints);

            await using var verify = new MainContext(options);
            Assert.False(
                await verify.CharacterInventories.AnyAsync(i => i.CharacterId == 9004, ct)
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Buy_RefusesCurrencyTheMachineDoesNotTake()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            var user = CreateUserWithCharacter(5, 9005, aiPoints: 1000, nicoPoints: 1000);
            int machineId;
            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                machineId = await SeedMachineAsync(db, aiPrice: 100, nicoPrice: 0);
            }

            await using var runDb = new MainContext(options);
            var session = CreateSession(user, machineId);
            var handler = CreateBuyHandler(runDb);
            await handler.HandleAsync(BuildBuyPayload(GachaBuyType.NicoPoints), session, ct);
            await handler.HandleAsync(BuildBuyPayload(GachaBuyType.Ticket), session, ct);

            Assert.Equal(2, session.Sent.Count);
            Assert.All(
                session.Sent,
                p =>
                    Assert.Equal(
                        AreaGachaBuyHandler.RefusedResult,
                        GachaBuyResponse.FromBytes(p.Payload).Result
                    )
            );
            Assert.Equal(1000, session.User!.AiPoints);
            Assert.Equal(1000, session.User.NicoPoints);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Buy_RefusesWithoutAnOpenMachine()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            var user = CreateUserWithCharacter(6, 9006, aiPoints: 1000, nicoPoints: 0);
            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                await SeedMachineAsync(db, aiPrice: 100, nicoPrice: 0);
            }

            await using var runDb = new MainContext(options);
            var session = CreateSession(user, machineId: null);
            await CreateBuyHandler(runDb)
                .HandleAsync(BuildBuyPayload(GachaBuyType.AiPoints), session, ct);

            var buy = GachaBuyResponse.FromBytes(Assert.Single(session.Sent).Payload);
            Assert.Equal(AreaGachaBuyHandler.RefusedResult, buy.Result);
            Assert.Equal(1000, session.User!.AiPoints);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Buy_RefusesWhenBagIsFull_AndKeepsTheMoney()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            var user = CreateUserWithCharacter(7, 9007, aiPoints: 1000, nicoPoints: 0);
            int machineId;
            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                machineId = await SeedMachineAsync(db, aiPrice: 100, nicoPrice: 0);
                for (var index = 0; index < CharacterRepository.MaximumInventoryStacks; index++)
                {
                    var itemId = 34_000_000 + index;
                    db.Items.Add(new Item { Id = itemId, Name = $"Filler {index}" });
                    db.CharacterInventories.Add(
                        new CharacterInventory
                        {
                            CharacterId = 9007,
                            ItemId = itemId,
                            Quantity = 1,
                        }
                    );
                }
                await db.SaveChangesAsync(ct);
            }

            await using var runDb = new MainContext(options);
            var session = CreateSession(user, machineId);
            await CreateBuyHandler(runDb)
                .HandleAsync(BuildBuyPayload(GachaBuyType.AiPoints), session, ct);

            var buy = GachaBuyResponse.FromBytes(Assert.Single(session.Sent).Payload);
            Assert.Equal(AreaGachaBuyHandler.RefusedResult, buy.Result);
            Assert.Equal(1000, session.User!.AiPoints);

            await using var verify = new MainContext(options);
            Assert.Equal(1000, (await verify.Users.SingleAsync(u => u.Id == 7, ct)).AiPoints);
            Assert.False(
                await verify.CharacterInventories.AnyAsync(
                    i => i.CharacterId == 9007 && i.ItemId == PrizeItemId,
                    ct
                )
            );
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task End_ClosesMachineAndAcknowledges()
    {
        var session = new CapturingPlayerSession
        {
            CharacterId = 9001,
            User = new User { Id = 1, Username = "gacha-end" },
            ActiveGachaMachineId = 3,
        };
        await new AreaGachaEndHandler(NullLogger<AreaGachaEndHandler>.Instance).HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Null(session.ActiveGachaMachineId);
        Assert.Equal(
            [PacketType.GachaEndResponse, PacketType.GachaEndedNotify],
            session.Sent.Select(p => p.Type).ToArray()
        );
        Assert.Equal([0, 0, 0, 0], session.Sent[0].Payload);
    }

    [Fact]
    public async Task TicketExchange_RefusesDepositsAndWithdrawals()
    {
        var session = new CapturingPlayerSession { CharacterId = 9001 };
        var writer = new PacketWriter();
        writer.Write(10100220u);
        writer.Write((ushort)1);
        var ct = TestContext.Current.CancellationToken;

        await new AreaGachaTicketExchangeItemAddHandler(
            NullLogger<AreaGachaTicketExchangeItemAddHandler>.Instance
        ).HandleAsync(writer.ToBytes(), session, ct);
        await new AreaGachaTicketExchangeItemDelHandler(
            NullLogger<AreaGachaTicketExchangeItemDelHandler>.Instance
        ).HandleAsync(writer.ToBytes(), session, ct);
        await new AreaGachaTicketExchangeCloseHandler().HandleAsync(new byte[4], session, ct);

        Assert.Equal(
            [
                PacketType.GachaTicketExchangeItemAddResponse,
                PacketType.GachaTicketExchangeItemDelResponse,
            ],
            session.Sent.Select(p => p.Type).ToArray()
        );
        Assert.All(session.Sent, p => Assert.Equal(1u, new PacketReader(p.Payload).ReadUInt()));
    }

    private static async Task<int> SeedMachineAsync(
        MainContext db,
        long aiPrice,
        long nicoPrice,
        int? ticketItemId = null,
        int ticketsPerPull = 1,
        bool isEnabled = true
    )
    {
        var ct = TestContext.Current.CancellationToken;
        db.Items.Add(
            new Item
            {
                Id = PrizeItemId,
                Name = "野球帽(黒)",
                Socket = 2,
            }
        );
        db.Items.Add(
            new Item
            {
                Id = TicketItemId,
                Name = "無地ソックス(白)",
                Socket = 128,
            }
        );
        var machine = new GachaMachine
        {
            Code = "akihabara_station_aipon",
            DisplayName = "aiぽん",
            VisualId = 100000,
            AiPrice = aiPrice,
            NicoPrice = nicoPrice,
            TicketItemId = ticketItemId,
            TicketsPerPull = ticketsPerPull,
            IsEnabled = isEnabled,
        };
        machine.Prizes.Add(
            new GachaPrize
            {
                ItemId = PrizeItemId,
                Weight = 1,
                HitType = 1,
            }
        );
        db.GachaMachines.Add(machine);
        db.Npcs.Add(
            new Npc
            {
                MapId = StationMapId,
                NpcObjectId = MachineNpcObjectId,
                ModelId = 8800100,
                Name = "aiぽん",
                NamePlate = 0,
                X = -8950f,
                Y = 2f,
                Z = -17850f,
                Rotation = 225,
                InteractionType = NpcInteractionType.Gacha,
                GachaMachine = machine,
                IsEnabled = true,
            }
        );
        await db.SaveChangesAsync(ct);
        return machine.Id;
    }

    private static CapturingPlayerSession CreateSession(User user, int? machineId) =>
        new()
        {
            User = user,
            UserId = user.Id,
            CharacterId = (uint)user.Characters.Single().Id,
            Character = user.Characters.Single(),
            MapId = StationMapId,
            ActiveGachaMachineId = machineId,
        };

    private static AreaGachaBuyHandler CreateBuyHandler(MainContext db) =>
        new(
            db,
            new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
            new GachaRepository(db),
            NullLogger<AreaGachaBuyHandler>.Instance
        );

    private static AreaEventAccessNpcHandler CreateEventAccessNpcHandler(MainContext db)
    {
        var eventRepository = new CharacterEventRepository(db);
        var serverScriptSession = new ServerScriptSession(
            eventRepository,
            NullLogger<ServerScriptSession>.Instance
        );
        return new AreaEventAccessNpcHandler(
            new NpcRepository(db),
            new ShopRepository(db),
            new ServerScriptDispatcher(
                [],
                serverScriptSession,
                NullLogger<ServerScriptDispatcher>.Instance
            ),
            new AdventureShopCatalog(new AdventureShopRepository(db)),
            TestTextLocaliser.English,
            NullLogger<AreaEventAccessNpcHandler>.Instance,
            new DramaCatalog(db, TestTextLocaliser.English)
        );
    }

    private static User CreateUserWithCharacter(
        int userId,
        int characterId,
        long aiPoints,
        long nicoPoints
    )
    {
        var user = new User
        {
            Id = userId,
            Username = $"gacha-user-{userId}",
            AiPoints = aiPoints,
            NicoPoints = nicoPoints,
        };
        user.SetPassword("pw");
        user.Characters.Add(
            new Character
            {
                Id = characterId,
                Name = $"Gacha Character {characterId}",
                UserId = userId,
                CurrentMapId = StationMapId,
                ModelId = 100,
                Birthdate = new DateTime(2000, 1, 2),
            }
        );
        return user;
    }

    private static byte[] BuildBuyPayload(GachaBuyType buyType)
    {
        var writer = new PacketWriter();
        writer.Write((uint)buyType);
        return writer.ToBytes();
    }

    private static byte[] BuildEventAccessNpcPayload(uint npcObjectId)
    {
        var writer = new PacketWriter();
        writer.Write(npcObjectId);
        writer.Write(-9000f);
        writer.Write(2f);
        writer.Write(-17900f);
        return writer.ToBytes();
    }
}
