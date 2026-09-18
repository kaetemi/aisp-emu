using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;

namespace aisp.Common.Tests;

public class NiconiCommonsAudioIconTests
{
    [Fact]
    public async Task Base_lists_are_empty_for_the_july_2009_client()
    {
        var session = new CapturingPlayerSession();
        var ct = TestContext.Current.CancellationToken;
        await new AreaNiconiCommonsBaseListHandler().HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            ct
        );
        var commons = Assert.Single(session.Sent).Payload;
        var reader = new PacketReader(commons);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());

        session.Sent.Clear();
        await new AreaUccVoiceBaseListHandler().HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            ct
        );
        var voices = Assert.Single(session.Sent).Payload;
        var voiceReader = new PacketReader(voices);
        Assert.Equal(0u, voiceReader.ReadUInt());
        Assert.Equal(0u, voiceReader.ReadUInt());
    }
}
