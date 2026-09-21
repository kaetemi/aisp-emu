using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class TimeZoneGetResponseTests
{
    [Fact]
    public void September2008_TimeZone_IsFourUints()
    {
        var bytes = new TimeZoneGetResponse(0, 2, 30, 100, 1).ToSeptember2008Bytes();
        Assert.Equal(new byte[] { 0, 0, 0, 0, 2, 0, 0, 0, 30, 0, 0, 0, 100, 0, 0, 0 }, bytes);
    }
}
