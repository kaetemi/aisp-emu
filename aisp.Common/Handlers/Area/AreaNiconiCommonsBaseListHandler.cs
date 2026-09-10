using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>The titles of the drama notebook's carousel: one per figure box this character has a doll in.</summary>
public sealed class AreaNiconiCommonsBaseListHandler(ICharacterRepository characters)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.NiconiCommonsBaseListRequest;

    public PacketType ResponseType => PacketType.NiconiCommonsBaseListResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var character =
            session.CharacterId == 0
                ? null
                : await characters.GetByIdAsync(checked((int)session.CharacterId), ct);
        var response = new NiconiCommonsBaseListResponse(
            0,
            [
                .. DramaFigures.TitlesOwnedBy(character),
                .. NiconiCommonsShopCatalog.CommonsRows.Select(row => new NiconiCommonsEntry
                {
                    Id = row.Id,
                    IconId = NiconiCommonsShopCatalog.IconIdFor(row),
                    Type = row.Type,
                    Name = NiconiCommonsShopCatalog.CommonsName(row.Id),
                    Available =
                        character?.Inventory.Any(stack =>
                            stack.ItemId == NiconiCommonsShopCatalog.CommonsBagItemId(row.Id)
                            && stack.Quantity > 0
                        ) == true,
                }),
            ]
        );
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
