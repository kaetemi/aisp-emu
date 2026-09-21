using System.Buffers.Binary;
using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Common.Handlers.Common;
using aisp.Common.Handlers.Msg;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Msg;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class AvatarGetCreateInfoHandlerTests
{
    [Fact]
    public async Task September2008LobbyVersionCheck_SelectsFourListCreateInfo()
    {
        var session = new CapturingPlayerSession();
        var version = new PacketWriter();
        version.Write(0u);
        version.Write(ClientWireProfile.September2008LobbyCrc);
        version.Write(3u);
        await new MsgVersionCheckHandler().HandleAsync(
            version.ToBytes(),
            session,
            TestContext.Current.CancellationToken
        );

        await new AvatarGetCreateInfoHandler().HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        var create = session.Sent.Last();
        Assert.Equal(PacketType.AvatarGetCreateInfoResponse, create.Type);
        Assert.Equal(4u, BinaryPrimitives.ReadUInt32LittleEndian(create.Payload));
        Assert.Equal(0, create.Payload[4]);
    }

    [Fact]
    public async Task July2009VersionCheck_KeepsBuildListCreateInfo()
    {
        var session = new CapturingPlayerSession();
        var version = new PacketWriter();
        version.Write(0u);
        version.Write(0xA98E12D0u);
        version.Write(0x03FA6EC0u);
        await new MsgVersionCheckHandler().HandleAsync(
            version.ToBytes(),
            session,
            TestContext.Current.CancellationToken
        );

        await new AvatarGetCreateInfoHandler().HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        var create = session.Sent.Last();
        Assert.Equal(3u, BinaryPrimitives.ReadUInt32LittleEndian(create.Payload));
        Assert.Equal(1001021u, BinaryPrimitives.ReadUInt32LittleEndian(create.Payload.AsSpan(4)));
    }

    [Fact]
    public void AreaVersionCheck_Uses2008ChangeMapOpcode()
    {
        var session = new CapturingPlayerSession();
        ClientWireProfile.RememberVersionCheck(session, ClientWireProfile.September2008AreaCrc, 2);
        Assert.Equal(PacketType.RoboRestResponse, ClientWireProfile.NotifyChangeMapOpcode(session));
        Assert.Equal(0xB235, (ushort)ClientWireProfile.NotifyChangeMapOpcode(session));
    }

    [Fact]
    public async Task September2008_EmptyAvatarList_IsResultZero_WithoutAvatarData()
    {
        var session = new CapturingPlayerSession { User = new User { Id = 1 } };
        ClientWireProfile.RememberVersionCheck(session, ClientWireProfile.September2008LobbyCrc, 3);
        var handler = new AvatarGetDataHandler(NullLogger<AvatarGetDataHandler>.Instance, null!);

        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        var only = Assert.Single(session.Sent);
        Assert.Equal(PacketType.AvatarGetDataResponse, only.Type);
        Assert.Equal(0u, new PacketReader(only.Payload).ReadUInt());
    }

    [Fact]
    public async Task September2008_AvatarList_SendsRecordThenReady()
    {
        var character = new Character
        {
            Id = 42,
            Name = "eina",
            ModelId = 1_002_011,
            BloodType = BloodType.A,
            Birthdate = new DateTime(2008, 9, 8),
            Gender = 2,
            FaceType = 2,
            Hairstyle = 10_930_020,
            HomeIslandId = 3,
            Equipment = new List<CharacterEquipment>
            {
                new() { SlotIndex = 0, ItemId = 10_100_060 },
            },
        };
        var session = new CapturingPlayerSession
        {
            User = new User
            {
                Id = 1,
                Characters = new List<Character> { character },
            },
        };
        ClientWireProfile.RememberVersionCheck(session, ClientWireProfile.September2008LobbyCrc, 3);
        var handler = new AvatarGetDataHandler(NullLogger<AvatarGetDataHandler>.Instance, null!);

        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(2, session.Sent.Count);
        Assert.Equal(PacketType.AvatarDataResponse, session.Sent[0].Type);
        Assert.Equal(0x6747, (ushort)session.Sent[0].Type);
        Assert.DoesNotContain(
            session.Sent,
            packet => packet.Type == PacketType.AvatarDestroyResponse
        );
        var reader = new PacketReader(session.Sent[0].Payload);
        Assert.Equal(42u, reader.ReadUInt());
        Assert.Equal("eina", reader.ReadString());
        var visual = aisp.Network.Data.CharaVisual.FromBytes(reader.ReadBytes(19));
        Assert.Equal(2u, visual.Gender);
        Assert.Equal(2, visual.Face);
        Assert.Equal(10_930_020u, visual.Hairstyle);
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(10_100_060u, reader.ReadUInt());
        Assert.Equal(28 * 4, reader.Remaining);
        Assert.Equal(PacketType.AvatarGetDataResponse, session.Sent[1].Type);
        Assert.Equal(0u, new PacketReader(session.Sent[1].Payload).ReadUInt());
    }
}
