using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaEmotionGetObtainedListHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EmotionGetObtainedListRequest;
    public PacketType ResponseType => PacketType.EmotionGetObtainedListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var ids = new List<uint>();

        // July 2009 emotion table is ids 1–27 (see AreaEmotionGetBaseListHandler).
        // 2011 extras (28–36, 100–105, 10101000+ voices) abort the 2009 client when the HUD loads.
        for (uint i = 1; i <= 27; i++)
            ids.Add(i);

        var response = new EmotionGetObtainedListResponse(0, ids);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
