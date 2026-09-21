using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class AreasvEnterResponseTests
{
    [Fact]
    public void September2008_EnterResponse_IsTheResultAlone()
    {
        var bytes = new AreasvEnterResponse(0, 42).ToSeptember2008Bytes();
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, bytes);
    }

    [Fact]
    public void LaterClients_EnterResponse_IsResultThenObjectId()
    {
        var bytes = new AreasvEnterResponse(0, 42).ToBytes();
        Assert.Equal(new byte[] { 0, 0, 0, 0, 42, 0, 0, 0 }, bytes);
    }
}
