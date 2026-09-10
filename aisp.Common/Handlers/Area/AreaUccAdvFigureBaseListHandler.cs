using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>The dolls the drama notebook's figure picker offers this character (see <see cref="DramaFigures"/>).</summary>
public sealed class AreaUccAdvFigureBaseListHandler(ICharacterRepository characters)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.UccAdvFigureBaseListRequest;

    public PacketType ResponseType => PacketType.UccAdvFigureBaseListResponse;

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
        // 0x47a900 indexes all definitions; the notebook truncates ids at 0x57e741.
        // Use the same 16-bit id for notebook, shop, and obtain; inventory keeps ItemId.
        var response = new UccAdvFigureBaseListResponse(
            0,
            [
                .. DramaFigures.AlwaysGranted,
                .. DramaFigures.Purchasable.Select(paid => new UccAdvFigure
                {
                    FigureId = paid.Figure.FigureId,
                    IconId = paid.Figure.IconId,
                    BoxId = paid.Figure.BoxId,
                    Name = paid.Figure.Name,
                    Owned =
                        character?.Inventory.Any(stack =>
                            stack.ItemId == (int)paid.ItemId && stack.Quantity > 0
                        ) == true,
                    Gender = paid.Figure.Gender,
                    People = paid.Figure.People,
                    PackageId = paid.Figure.PackageId,
                    ModelId = paid.Figure.ModelId,
                    Face = paid.Figure.Face,
                    Hairstyle = paid.Figure.Hairstyle,
                    Equipment = paid.Figure.Equipment,
                }),
            ]
        );
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
