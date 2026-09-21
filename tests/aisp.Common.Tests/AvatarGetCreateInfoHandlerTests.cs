using System.Buffers.Binary;
using aisp.Common.Game;
using aisp.Common.Handlers.Common;
using aisp.Common.Handlers.Msg;
using aisp.Common.Tests.Support;
using aisp.Network;

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
}
