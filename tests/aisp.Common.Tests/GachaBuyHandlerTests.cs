using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class GachaBuyHandlerTests
{
    [Fact]
    public async Task Buy_DebitsDereAndGrantsPrize()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var ct = TestContext.Current.CancellationToken;
            const int itemId = (int)GachaTestSession.DefaultPrizeItemId;
            var user = new User
            {
                Id = 1,
                Username = "gacha-buy",
                AiPoints = 500,
            };
            user.SetPassword("pw");
            user.Characters.Add(
                new Character
                {
                    Id = 9,
                    Name = "Kaetemi",
                    UserId = 1,
                    CurrentMapId = 10990100,
                    ModelId = 100,
                    Birthdate = new DateTime(2000, 1, 2),
                }
            );
            await using (var db = new MainContext(options))
            {
                db.Users.Add(user);
                db.Items.Add(new Item { Id = itemId, Name = "Gacha Prize" });
                await db.SaveChangesAsync(ct);
            }

            GachaTestSession.AiPrice = 100;
            GachaTestSession.NicoPrice = 0;
            GachaTestSession.PrizeItemId = (uint)itemId;

            var session = new CapturingPlayerSession
            {
                User = user,
                UserId = 1,
                Character = user.Characters.First(),
                CharacterId = 9,
                MapId = 10990100,
            };
            var handler = new AreaGachaBuyHandler(
                new UserRepository(new MainContext(options)),
                new CharacterRepository(
                    new MainContext(options),
                    NullLogger<CharacterRepository>.Instance
                ),
                NullLogger<AreaGachaBuyHandler>.Instance
            );

            var writer = new PacketWriter();
            writer.Write(0u);
            await handler.HandleAsync(writer.ToBytes(), session, ct);

            var buy = Assert.Single(session.Sent, p => p.Type == PacketType.GachaBuyResponse);
            var parsed = GachaBuyResponse.FromBytes(buy.Payload);
            Assert.Equal(0u, parsed.Result);
            Assert.Equal((uint)itemId, parsed.SerialId);
            Assert.Equal((ushort)1, parsed.Num);
            Assert.Equal(GachaTestSession.PrizeHitType, parsed.HitType);
            Assert.Contains(session.Sent, p => p.Type == PacketType.MoneyUpdatedAipoint);
            Assert.Contains(session.Sent, p => p.Type == PacketType.ItemCreateNotify);
            Assert.Equal(400, session.User!.AiPoints);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
