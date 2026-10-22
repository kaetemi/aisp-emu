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

public sealed class HotaruServerScriptTests
{
    private const uint StationMapId = 10990100;
    private const long HotaruObjectId = 1342177289;
    private const int PrizeItemId = 10000060;
    private const int TicketItemId = 20000135;

    [Fact]
    public async Task RegisteredPlayer_RecyclesThreePrizesThroughTheBox()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            await using var db = new MainContext(options);
            var npc = await SeedAsync(db, homeIslandId: 1, prizeCount: 3);
            var (script, dispatcher, exchange, scriptSession) = Create(db);
            var session = new CapturingPlayerSession
            {
                CharacterId = 9020,
                UserId = 20,
                User = await db.Users.SingleAsync(u => u.Id == 20, ct),
                MapId = StationMapId,
            };

            await dispatcher.StartAsync(
                session,
                ServerEvents.Keys.Hotaru,
                new ServerScriptContext { Npc = npc },
                EventCompletionPolicy.Replayable,
                ct
            );
            Assert.Equal(
                [
                    PacketType.EventStartNotify,
                    PacketType.EventSelectInitNotify,
                    PacketType.EventSelectPushNotify,
                    PacketType.EventSelectPushNotify,
                    PacketType.EventSelectExecNotify,
                ],
                session.Sent.Select(p => p.Type).ToArray()
            );
            session.Sent.Clear();

            // リサイクルする → the standalone box opens with the machine's quota.
            Assert.True(
                await dispatcher.TryHandlePacketAsync(
                    PacketType.EventSelectExecRRequest,
                    SelectPayload(0, 0),
                    session,
                    ct
                )
            );
            var open = Assert.Single(session.Sent);
            Assert.Equal(PacketType.GachaTicketExchangeOpenNotify, open.Type);
            Assert.Equal(3u, GachaTicketExchangeOpenNotify.FromBytes(open.Payload).BaseNum);
            Assert.NotNull(session.GachaTicketBox);
            Assert.NotNull(session.ActiveGachaMachineId);
            session.Sent.Clear();

            Assert.True(await exchange.DepositAsync(session, PrizeItemId, 3, ct));
            session.Sent.Clear();

            // Closing the box settles it and Hotaru hands over the ticket.
            await new AreaGachaTicketExchangeCloseHandler(
                exchange,
                NullLogger<AreaGachaTicketExchangeCloseHandler>.Instance,
                dispatcher
            ).HandleAsync(UIntPayload(1), session, ct);

            Assert.Null(session.GachaTicketBox);
            Assert.Null(session.ActiveGachaMachineId);
            Assert.Contains(session.Sent, p => p.Type == PacketType.ItemCreateNotify);
            var message = Assert.Single(session.Sent, p => p.Type == PacketType.EventMessageNotify);
            var reader = new PacketReader(message.Payload);
            Assert.Equal((uint)HotaruObjectId, reader.ReadUInt());
            Assert.Equal("Hotaru", reader.ReadString());
            Assert.Contains("1", reader.ReadString());
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventSyncNotify);
            session.Sent.Clear();

            Assert.True(
                await dispatcher.TryHandlePacketAsync(
                    PacketType.EventSyncRRequest,
                    UIntPayload(0),
                    session,
                    ct
                )
            );
            Assert.Contains(session.Sent, p => p.Type == PacketType.EventEndNotify);
            Assert.Null(session.ActiveEventKey);

            await using var verify = new MainContext(options);
            Assert.Equal(
                1,
                (
                    await verify.CharacterInventories.SingleAsync(
                        i => i.CharacterId == 9020 && i.ItemId == TicketItemId,
                        ct
                    )
                ).Quantity
            );
            Assert.False(
                await verify.CharacterInventories.AnyAsync(
                    i => i.CharacterId == 9020 && i.ItemId == PrizeItemId,
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
    public async Task UnregisteredPlayer_GetsTheTutorialScript()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            await using var db = new MainContext(options);
            var npc = await SeedAsync(db, homeIslandId: 0, prizeCount: 0);
            var (_, dispatcher, _, _) = Create(db);
            var session = new CapturingPlayerSession
            {
                CharacterId = 9020,
                UserId = 20,
                MapId = StationMapId,
            };

            await dispatcher.StartAsync(
                session,
                ServerEvents.Keys.Hotaru,
                new ServerScriptContext { Npc = npc },
                EventCompletionPolicy.Replayable,
                ct
            );

            Assert.Equal(
                [PacketType.EventStartNotify, PacketType.EventScriptPlayNotify],
                session.Sent.Select(p => p.Type).ToArray()
            );
            Assert.Equal(
                new EventScriptPlayNotify(
                    ScriptedEvents.GetScriptLabel(ScriptedEvents.Keys.IntroductionHotaru0)
                ).ToBytes(),
                session.Sent[1].Payload
            );
            Assert.Equal(NpcEventKind.ClientScript, session.ActiveEventKind);
            Assert.Equal(ScriptedEvents.Keys.IntroductionHotaru0, session.ActiveEventKey);
            Assert.Null(session.ServerScriptState);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task DecliningEndsTheEvent()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            await using var db = new MainContext(options);
            var npc = await SeedAsync(db, homeIslandId: 2, prizeCount: 0);
            var (_, dispatcher, _, _) = Create(db);
            var session = new CapturingPlayerSession
            {
                CharacterId = 9020,
                UserId = 20,
                MapId = StationMapId,
            };
            await dispatcher.StartAsync(
                session,
                ServerEvents.Keys.Hotaru,
                new ServerScriptContext { Npc = npc },
                EventCompletionPolicy.Replayable,
                ct
            );
            session.Sent.Clear();

            Assert.True(
                await dispatcher.TryHandlePacketAsync(
                    PacketType.EventSelectExecRRequest,
                    SelectPayload(0, 1),
                    session,
                    ct
                )
            );
            Assert.Equal([PacketType.EventEndNotify], session.Sent.Select(p => p.Type).ToArray());
            Assert.Null(session.GachaTicketBox);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task NpcSeed_LinksHotaruToTheStationMachine()
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
            await GachaRepository.SeedFromJsonAsync(
                db,
                Path.Combine(seedDirectory, "gachaMachines.json"),
                ct: ct
            );
            await NpcRepository.SeedFromJsonAsync(
                db,
                Path.Combine(seedDirectory, "npcs.json"),
                ct: ct
            );

            var hotaru = await db.Npcs.SingleAsync(x => x.NpcObjectId == HotaruObjectId, ct);
            var machine = await db.GachaMachines.SingleAsync(
                x => x.Code == "akihabara_station_aipon",
                ct
            );
            Assert.Equal(machine.Id, hotaru.GachaMachineId);
            Assert.Equal(NpcEventKind.ServerScript, hotaru.EventKind);
            Assert.Equal(ServerEvents.Keys.Hotaru, hotaru.EventKey);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static async Task<Npc> SeedAsync(MainContext db, uint homeIslandId, int prizeCount)
    {
        var ct = TestContext.Current.CancellationToken;
        var user = new User { Id = 20, Username = "hotaru-user" };
        user.SetPassword("pw");
        user.Characters.Add(
            new Character
            {
                Id = 9020,
                Name = "Hotaru Tester",
                UserId = 20,
                CurrentMapId = StationMapId,
                HomeIslandId = homeIslandId,
                Birthdate = new DateTime(2000, 1, 2),
            }
        );
        db.Users.Add(user);
        db.Items.Add(
            new Item
            {
                Id = PrizeItemId,
                Name = "野球帽(黒)",
                Socket = 2,
            }
        );
        db.Items.Add(new Item { Id = TicketItemId, Name = "aiポンチケット" });
        var machine = new GachaMachine
        {
            Code = "akihabara_station_aipon",
            DisplayName = "aiぽん",
            VisualId = 100000,
            AiPrice = 100,
            TicketItemId = TicketItemId,
            TicketsPerPull = 1,
            RecycleItemsPerTicket = 3,
        };
        machine.Prizes.Add(new GachaPrize { ItemId = PrizeItemId, Weight = 1 });
        db.GachaMachines.Add(machine);
        var npc = new Npc
        {
            MapId = StationMapId,
            NpcObjectId = HotaruObjectId,
            ModelId = 3992021,
            Name = "Hotaru",
            X = -9288f,
            Y = 2f,
            Z = -22225f,
            Rotation = 90,
            InteractionType = NpcInteractionType.Decorative,
            EventKind = NpcEventKind.ServerScript,
            EventKey = ServerEvents.Keys.Hotaru,
            GachaMachine = machine,
        };
        db.Npcs.Add(npc);
        if (prizeCount > 0)
            db.CharacterInventories.Add(
                new CharacterInventory
                {
                    CharacterId = 9020,
                    ItemId = PrizeItemId,
                    Quantity = prizeCount,
                }
            );
        await db.SaveChangesAsync(ct);
        return npc;
    }

    private static (
        HotaruServerScript Script,
        ServerScriptDispatcher Dispatcher,
        GachaTicketExchange Exchange,
        ServerScriptSession Session
    ) Create(MainContext db)
    {
        var scriptSession = new ServerScriptSession(
            new CharacterEventRepository(db),
            NullLogger<ServerScriptSession>.Instance
        );
        var gachaRepository = new GachaRepository(db);
        var exchange = new GachaTicketExchange(
            db,
            gachaRepository,
            NullLogger<GachaTicketExchange>.Instance
        );
        var script = new HotaruServerScript(
            new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
            gachaRepository,
            exchange,
            scriptSession,
            TestTextLocaliser.English,
            NullLogger<HotaruServerScript>.Instance
        );
        var dispatcher = new ServerScriptDispatcher(
            [script],
            scriptSession,
            NullLogger<ServerScriptDispatcher>.Instance
        );
        return (script, dispatcher, exchange, scriptSession);
    }

    private static byte[] SelectPayload(uint result, byte selectNo)
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(selectNo);
        return writer.ToBytes();
    }

    private static byte[] UIntPayload(uint value)
    {
        var writer = new PacketWriter();
        writer.Write(value);
        return writer.ToBytes();
    }
}
