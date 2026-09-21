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
            // 0x6747 is this build's avatar record, reached by subtracting 0x6719
            // and 0x2e from the opcode rather than a direct compare. The body has
            // no model id and 29 item ids. 0x6587 is a different 4-byte packet;
            // a long body there fails the consume check and closes Msg.
            // 0xB055 then moves the login scene: 0 to the maker, 100 to state 0x3E8.
            var ready = session.User!.Characters.Count != 0;
            if (ready)
            {
                var record = CreateDataResponse(session.User.Characters.First(), 0);
                await session.SendAsync(
                    PacketType.AvatarDataResponse,
                    record.ToSeptember2008Bytes(),
                    ct
                );
            }

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
