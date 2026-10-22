using System.Text;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class GachaPacketTests
{
    [Fact]
    public void GachaStartedNotify_WritesNameVisualPricesAndTicket()
    {
        var payload = new GachaStartedNotify("aiぽん", 100000, 100, 0, 10400000, 3).ToBytes();
        var parsed = GachaStartedNotify.FromBytes(payload);
        Assert.Equal("aiぽん", parsed.Name);
        Assert.Equal(100000u, parsed.VisualId);
        Assert.Equal(100ul, parsed.AiPrice);
        Assert.Equal(0ul, parsed.NicoPrice);
        Assert.Equal(10400000u, parsed.TicketItemId);
        Assert.Equal((ushort)3, parsed.TicketsPerPull);
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
    public void GachaStartedNotify_DefaultsToNoTicket()
    {
        var parsed = GachaStartedNotify.FromBytes(
            new GachaStartedNotify("machine", 100003, 0, 500).ToBytes()
        );
        Assert.Equal(0u, parsed.TicketItemId);
        Assert.Equal((ushort)0, parsed.TicketsPerPull);
        Assert.Equal(500ul, parsed.NicoPrice);
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

    [Theory]
    [InlineData(0u, GachaBuyType.AiPoints)]
    [InlineData(1u, GachaBuyType.NicoPoints)]
    [InlineData(2u, GachaBuyType.Ticket)]
    public void GachaBuyRequest_ReadsBuyType(uint wire, GachaBuyType expected)
    {
        var writer = new PacketWriter();
        writer.Write(wire);
        Assert.Equal(expected, GachaBuyRequest.FromBytes(writer.ToBytes()).BuyType);
    }

    [Fact]
    public void GachaEndPackets_HaveExpectedSizes()
    {
        Assert.Equal(4, new GachaEndResponse(0).ToBytes().Length);
        Assert.Empty(new GachaEndedNotify().ToBytes());
        _ = GachaEndRequest.FromBytes([]);
    }

    [Fact]
    public void GachaTicketExchangeOpenNotify_WritesQuota()
    {
        var payload = new GachaTicketExchangeOpenNotify(100).ToBytes();
        Assert.Equal(4, payload.Length);
        Assert.Equal(100u, GachaTicketExchangeOpenNotify.FromBytes(payload).BaseNum);
    }

    [Fact]
    public void GachaTicketExchangeItemRequests_ReadSerialAndNum()
    {
        var writer = new PacketWriter();
        writer.Write(10100220u);
        writer.Write((ushort)3);
        var add = GachaTicketExchangeItemAddRequest.FromBytes(writer.ToBytes());
        Assert.Equal(10100220u, add.SerialId);
        Assert.Equal((ushort)3, add.Num);
        var del = GachaTicketExchangeItemDelRequest.FromBytes(writer.ToBytes());
        Assert.Equal(10100220u, del.SerialId);
        Assert.Equal((ushort)3, del.Num);
    }

    [Fact]
    public void GachaTicketExchangeResponses_WriteResult()
    {
        Assert.Equal([1, 0, 0, 0], new GachaTicketExchangeItemAddResponse(1).ToBytes());
        Assert.Equal([0, 0, 0, 0], new GachaTicketExchangeItemDelResponse(0).ToBytes());
    }

    [Fact]
    public void GachaTicketExchangeCloseRequest_ReadsReason()
    {
        var writer = new PacketWriter();
        writer.Write(7u);
        Assert.Equal(7u, GachaTicketExchangeCloseRequest.FromBytes(writer.ToBytes()).Reason);
    }

    [Fact]
    public void GachaPacketTypes_UseClientOpcodes()
    {
        Assert.Equal(0xCC88, (ushort)PacketType.GachaStartedNotify);
        Assert.Equal(0x663A, (ushort)PacketType.GachaBuyRequest);
        Assert.Equal(0x2F2D, (ushort)PacketType.GachaBuyResponse);
        Assert.Equal(0xD7FC, (ushort)PacketType.GachaEndRequest);
        Assert.Equal(0x1380, (ushort)PacketType.GachaEndResponse);
        Assert.Equal(0x78A8, (ushort)PacketType.GachaEndedNotify);
        Assert.Equal(0x7D14, (ushort)PacketType.GachaTicketExchangeOpenNotify);
        Assert.Equal(0x45BD, (ushort)PacketType.GachaTicketExchangeItemAddRequest);
        Assert.Equal(0x7F77, (ushort)PacketType.GachaTicketExchangeItemAddResponse);
        Assert.Equal(0x3E26, (ushort)PacketType.GachaTicketExchangeItemDelRequest);
        Assert.Equal(0xC6DA, (ushort)PacketType.GachaTicketExchangeItemDelResponse);
        Assert.Equal(0x953B, (ushort)PacketType.GachaTicketExchangeCloseRequest);
    }
}
