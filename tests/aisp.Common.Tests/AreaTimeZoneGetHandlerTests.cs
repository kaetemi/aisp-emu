using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Tests;

public class AreaTimeZoneGetHandlerTests
{
    [Fact]
    public async Task September2008_TimeZoneGet_IsFourUints()
    {
        var session = new CapturingPlayerSession();
        ClientWireProfile.RememberVersionCheck(session, ClientWireProfile.September2008AreaCrc, 2);

        await new AreaTimeZoneGetHandler().HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        var packet = Assert.Single(session.Sent);
        Assert.Equal(PacketType.TimeZoneGetResponse, packet.Type);
        Assert.Equal(16, packet.Payload.Length);
    }

    [Fact]
    public async Task LaterClients_TimeZoneGet_KeepsTheFlagByte()
    {
        var session = new CapturingPlayerSession();

        await new AreaTimeZoneGetHandler().HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        var packet = Assert.Single(session.Sent);
        Assert.Equal(17, packet.Payload.Length);
        Assert.Equal(1, packet.Payload[^1]);
    }
}
