using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaNiconiCommonsBaseListHandler : IPacketHandler, IRequiresAuthenticatedSession
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
        // July 2009 client RST+logout after the 2011 Niconi Commons catalog.
        await session.SendAsync(
            ResponseType,
            new NiconiCommonsBaseListResponse(0, []).ToBytes(),
            ct
        );
    }
}
