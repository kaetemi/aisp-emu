using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaUccVoiceBaseListHandler(ICharacterRepository characters)
    : IPacketHandler,
        IRequiresAuthenticatedSession
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
        var character =
            session.CharacterId == 0
                ? null
                : await characters.GetByIdAsync(checked((int)session.CharacterId), ct);
        // 0x794d20 -> 0x47fa40 registers unowned rows too; 0x623df8 requires a known id.
        var response = new UccVoiceBaseListResponse(
            NiconiCommonsShopCatalog
                .VoiceRows.Select(row => new UccVoice(
                    row.Id,
                    NiconiCommonsShopCatalog.IconIdFor(row),
                    NiconiCommonsShopCatalog.VoiceName(row.Id),
                    character?.Inventory.Any(stack =>
                        stack.ItemId == NiconiCommonsShopCatalog.VoiceBagItemId(row.Id)
                        && stack.Quantity > 0
                    ) == true,
                    NiconiCommonsShopCatalog.VoiceName(row.Id)
                ))
                .ToArray()
        );
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
