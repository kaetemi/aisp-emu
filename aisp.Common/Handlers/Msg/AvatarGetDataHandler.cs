using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Msg;

public class AvatarGetDataHandler(
    ILogger<AvatarGetDataHandler> logger,
    ICharacterRepository charRepo
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AvatarGetDataRequest;

    public PacketType ResponseType => PacketType.AvatarDataResponse;

    public ServerType ServerType => ServerType.Msg;

    ILogger<AvatarGetDataHandler> _logger = logger;
    ICharacterRepository _charRepo = charRepo;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (ClientWireProfile.IsSeptember2008(session))
        {
            // 0x6747 is not in this exe. The record is 0x6587, then 0xB055:
            // 0 opens the maker, 100 opens character select. Any other value is an error.
            // Opcode 0x6587 is the record parser, but sending it (live 2026-09-21)
            // closes Msg. The list uint alone stays up. 0 and 100 both reach the
            // maker; 100 is the non-empty branch (scene state 0x3E8).
            var ready = session.User!.Characters.Count != 0;
            var listResult = ready
                ? ClientWireProfile.September2008AvatarListReady
                : ClientWireProfile.September2008AvatarListEmpty;
            await session.SendAsync(
                PacketType.AvatarGetDataResponse,
                new AvatarGetDataResponse(listResult).ToBytes(),
                ct
            );
            return;
        }

        if (session.User!.Characters.Count != 0)
        {
            Character cha = session.User!.Characters.First();

            var dataResponse = CreateDataResponse(cha, 0);
            await session.SendAsync(ResponseType, dataResponse.ToBytes(), ct);
        }
        var avatarGetDataResp = new AvatarGetDataResponse(0);
        await session.SendAsync(PacketType.AvatarGetDataResponse, avatarGetDataResp.ToBytes(), ct);
    }

    internal static AvatarDataResponse CreateDataResponse(Character character, uint slotId)
    {
        var dataResponse = new AvatarDataResponse(
            (uint)character.Id,
            character.Name,
            character.ModelId,
            character.HomeIslandId,
            slotId
        );
        dataResponse.Visual.VisualId = ResolveBuildId(character.ModelId);
        dataResponse.Visual.BloodType = character.BloodType;
        dataResponse.Visual.Month = (byte)character.Birthdate.Month;
        dataResponse.Visual.Day = (byte)character.Birthdate.Day;
        dataResponse.Visual.Gender = (uint)character.Gender;
        dataResponse.Visual.Face = (byte)character.FaceType;
        dataResponse.Visual.Hairstyle = character.Hairstyle;
        dataResponse.AddEquip(
            character.Equipment.Select(e => new CharacterEquipSlot(e.SlotIndex, (uint)e.ItemId)),
            ItemEntityMapper.ResolveEquipSocket
        );
        return dataResponse;
    }

    private static uint ResolveBuildId(uint modelId)
    {
        var buildId = modelId / 10 % 10;
        return buildId == 0 ? 1u : buildId;
    }
}
