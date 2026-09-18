using System.Text;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class GachaAndBoardPacketTests
{
    [Fact]
    public void GachaStartedNotify_WritesNameVisualPricesAndCatalog()
    {
        var payload = new GachaStartedNotify("aiぽん", 10110, 100, 0, 10100220, 1).ToBytes();
        var parsed = GachaStartedNotify.FromBytes(payload);
        Assert.Equal("aiぽん", parsed.Name);
        Assert.Equal(10110u, parsed.VisualId);
        Assert.Equal(100ul, parsed.AiPoint);
        Assert.Equal(0ul, parsed.NicoPoint);
        Assert.Equal(10100220u, parsed.ItemSerialId);
        Assert.Equal((ushort)1, parsed.ItemNum);
        Assert.Equal(
            Encoding.UTF8.GetByteCount("aiぽん")
                + 1
                + sizeof(uint)
                + sizeof(ulong)
                + sizeof(ulong)
                + sizeof(uint)
                + sizeof(ushort),
            payload.Length
        );
    }

    [Fact]
    public void GachaBuyResponse_IsElevenBytes()
    {
        var payload = new GachaBuyResponse(0, 10100220, 1, 2).ToBytes();
        Assert.Equal(GachaBuyResponse.WireSize, payload.Length);
        var parsed = GachaBuyResponse.FromBytes(payload);
        Assert.Equal(0u, parsed.Result);
        Assert.Equal(10100220u, parsed.SerialId);
        Assert.Equal((ushort)1, parsed.Num);
        Assert.Equal((byte)2, parsed.HitType);
    }

    [Fact]
    public void GachaBuyRequest_ReadsBuyType()
    {
        var writer = new PacketWriter();
        writer.Write(2u);
        var parsed = GachaBuyRequest.FromBytes(writer.ToBytes());
        Assert.Equal(2u, parsed.BuyType);
    }

    [Fact]
    public void EventBoardOpenNotify_WritesCStringUrl()
    {
        const string url = "http://127.0.0.1:8080/healthz";
        var payload = new EventBoardOpenNotify(url).ToBytes();
        Assert.Equal(url, EventBoardOpenNotify.FromBytes(payload).Url);
        Assert.Equal(Encoding.UTF8.GetByteCount(url) + 1, payload.Length);
    }

    [Fact]
    public void EventNoticeNotify_WritesNameTextTalkType()
    {
        var payload = new EventNoticeNotify("システム", "hello").ToBytes();
        var parsed = EventNoticeNotify.FromBytes(payload);
        Assert.Equal("システム", parsed.Name);
        Assert.Equal("hello", parsed.Text);
        Assert.Equal(0u, parsed.TalkType);
        Assert.Equal(
            Encoding.UTF8.GetByteCount("システム") + 1 + Encoding.UTF8.GetByteCount("hello") + 1,
            payload.Length
        );
    }

    [Fact]
    public void GachaTicketExchangeOpenNotify_WritesQuota()
    {
        var payload = new GachaTicketExchangeOpenNotify(10).ToBytes();
        Assert.Equal(4, payload.Length);
        Assert.Equal(10u, GachaTicketExchangeOpenNotify.FromBytes(payload).BaseNum);
    }

    [Fact]
    public void GachaTicketExchangeItemAddRequest_ReadsSerialAndNum()
    {
        var writer = new PacketWriter();
        writer.Write(10100220u);
        writer.Write((ushort)3);
        var parsed = GachaTicketExchangeItemAddRequest.FromBytes(writer.ToBytes());
        Assert.Equal(10100220u, parsed.SerialId);
        Assert.Equal((ushort)3, parsed.Num);
    }

    [Fact]
    public void GachaTicketExchangeItemAddResponse_WritesResult()
    {
        var payload = new GachaTicketExchangeItemAddResponse(0).ToBytes();
        Assert.Equal(4, payload.Length);
        Assert.Equal(0u, GachaTicketExchangeItemAddResponse.FromBytes(payload).Result);
    }

    [Fact]
    public void GachaEndedNotify_IsEmpty()
    {
        Assert.Empty(new GachaEndedNotify().ToBytes());
    }
}
