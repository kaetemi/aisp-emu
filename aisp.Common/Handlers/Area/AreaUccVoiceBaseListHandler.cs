using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaUccVoiceBaseListHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.UccVoiceBaseListRequest;
    public PacketType ResponseType => PacketType.UccVoiceBaseListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        // July 2009 client RST+logout after the 2011 drama-voice catalog.
        await session.SendAsync(ResponseType, new UccVoiceBaseListResponse([]).ToBytes(), ct);
    }
}
