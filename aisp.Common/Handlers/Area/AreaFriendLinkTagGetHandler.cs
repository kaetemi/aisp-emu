using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaFriendLinkTagGetHandler(IFriendRepository friends)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.FriendLinkTagGetRequest;
    public PacketType ResponseType => PacketType.FriendLinkTagGetResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = FriendLinkTagGetRequest.FromBytes(payload.Span);
        if (session.CharacterId > int.MaxValue)
        {
            await session.SendAsync(
                ResponseType,
                TagBody(session, new FriendLinkTagGetResponse(1, req.TargetObjectId)),
                ct
            );
            return;
        }

        var savedTags = await friends.GetLinkTagsAsync((int)session.CharacterId, ct);
        var populatedTags = savedTags
            .Where(tag => tag.Slot <= 4 && !string.IsNullOrWhiteSpace(tag.Name))
            .ToArray();
        var arbitraryTags = populatedTags
            .Select(tag => new FriendLinkTagData(
                FriendLinkTagCatalog.GetFreeTagId(tag.Name, tag.Slot),
                tag.Name
            ))
            .ToArray();
        var arbitrarySlots = populatedTags.Select(tag => tag.Slot).ToArray();

        var response = new FriendLinkTagGetResponse(
            0,
            req.TargetObjectId,
            arbitraryTags,
            arbitrarySlots,
            FriendLinkTagCatalog.QuestionnaireTags,
            Enumerable
                .Range(0, FriendLinkTagCatalog.QuestionnaireTags.Count)
                .Select(x => (uint)x)
                .ToArray()
        );
        await session.SendAsync(ResponseType, TagBody(session, response), ct);
    }

    private static byte[] TagBody(IPlayerSession session, FriendLinkTagGetResponse response) =>
        ClientWireProfile.IsSeptember2008(session) ? response.ToSeptember2008Bytes() : response.ToBytes();
}
