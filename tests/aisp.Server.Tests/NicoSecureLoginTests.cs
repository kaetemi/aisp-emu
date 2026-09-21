namespace aisp.Server.Tests;

public class NicoSecureLoginTests
{
    [Fact]
    public void OkXml_HasStatusOkAndTicket()
    {
        var xml = HttpEndpointsExtensions.NicoUserResponseOk("eina");
        Assert.Contains("status=\"ok\"", xml);
        Assert.Contains("<ticket>eina</ticket>", xml);
        Assert.StartsWith("<?xml", xml);
    }

    [Fact]
    public void OkXml_EmptyMail_UsesLocalTicket()
    {
        Assert.Contains("<ticket>local</ticket>", HttpEndpointsExtensions.NicoUserResponseOk(""));
    }
}
