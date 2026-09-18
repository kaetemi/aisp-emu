using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaUccAdvFigureBaseListHandler : IPacketHandler, IRequiresAuthenticatedSession
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
        // July 2009 client RST+logout after the 2011 377-byte figure records (~10 KB).
        await session.SendAsync(
            ResponseType,
            new UccAdvFigureBaseListResponse(0, []).ToBytes(),
            ct
        );
    }
}
