using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// September 2008 area <c>0xC8EA</c>. The reply is a result uint and a count.
/// Count 0 exact-consumes and clears the post-enter wait. Later clients ask
/// the lobby for this catalog, so a non-2008 area session is left unanswered.
/// </summary>
public class AreaItemGetBaseListHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemGetBaseListRequest;
    public PacketType ResponseType => PacketType.ItemGetBaseListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (!ClientWireProfile.IsSeptember2008(session))
            return;

        await session.SendAsync(ResponseType, new ItemGetBaseListResponse().ToBytes(), ct);
    }
}
