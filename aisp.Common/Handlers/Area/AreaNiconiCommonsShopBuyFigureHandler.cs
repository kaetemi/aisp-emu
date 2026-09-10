using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public sealed class AreaNiconiCommonsShopBuyFigureHandler(
    MainContext db,
    ICharacterRepository characters,
    ILogger<AreaNiconiCommonsShopBuyFigureHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.NiconiCommonsShopBuyFigureRequest;
    public PacketType ResponseType => PacketType.NiconiCommonsShopBuyResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = NiconiCommonsShopBuyFigureRequest.FromBytes(payload.Span);
        var row = NiconiCommonsShopCatalog.FigureRows.FirstOrDefault(row =>
            row.Id == request.FigureId
        );
        var paid = NiconiCommonsShopCatalog.PaidFigureForShopId(request.FigureId);
        if (row is null || paid is null)
        {
            logger.LogWarning(
                "NiconiCommonsShopBuyFigure unknown id {Id} from character {CharacterId}",
                request.FigureId,
                session.CharacterId
            );
            await NiconiCommonsShopPurchase.SendRefusalAsync(session, ct);
            return;
        }

        if (
            !await NiconiCommonsShopPurchase.TryChargeAndGrantBagItemAsync(
                db,
                session,
                row,
                request.Extra,
                checked((int)paid.ItemId),
                paid.Figure.Name,
                ct
            )
        )
        {
            await NiconiCommonsShopPurchase.SendRefusalAsync(session, ct);
            return;
        }

        await NiconiCommonsShopPurchase.SendPaidAsync(
            characters,
            session,
            PacketType.UccAdvFigureObtainNotify,
            new UccAdvFigureObtainNotify(request.FigureId).ToBytes(),
            ct
        );
    }
}
