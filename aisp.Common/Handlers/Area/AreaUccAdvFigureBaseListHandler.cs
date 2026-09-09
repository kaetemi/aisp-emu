using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
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
        var response = new UccAdvFigureBaseListResponse(0, DramaFigures.OwnedBy(character));
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
