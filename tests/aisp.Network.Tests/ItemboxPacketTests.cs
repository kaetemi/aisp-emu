using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class ItemboxPacketTests
{
    [Fact]
    public void ItemboxItemCreateNotify_IsTwentyTwoBytes()
    {
        var payload = new ItemboxItemCreateNotify(0x0102030405060708, 10000060, 3).ToBytes();
        Assert.Equal(ItemboxItemCreateNotify.WireSize, payload.Length);
        var parsed = ItemboxItemCreateNotify.FromBytes(payload);
        Assert.Equal(0x0102030405060708ul, parsed.EntryId);
        Assert.Equal(10000060u, parsed.SerialId);
        Assert.Equal((ushort)3, parsed.Num);
        Assert.Equal(0ul, parsed.Extra);
        Assert.Equal(0x08, payload[0]);
    }

    [Fact]
    public void ItemboxItemDeleteNotify_IsEightBytes()
    {
        var payload = new ItemboxItemDeleteNotify(42).ToBytes();
        Assert.Equal(ItemboxItemDeleteNotify.WireSize, payload.Length);
        Assert.Equal(42ul, ItemboxItemDeleteNotify.FromBytes(payload).EntryId);
    }

    [Fact]
    public void ItemBoxTakeoutRequest_ReadsTheEntryId()
    {
        var writer = new PacketWriter();
        writer.Write(7ul);
        Assert.Equal(7ul, ItemBoxTakeoutRequest.FromBytes(writer.ToBytes()).EntryId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ItemBoxTakeoutResponse_IsFourBytes(int result)
    {
        var payload = new ItemBoxTakeoutResponse(result).ToBytes();
        Assert.Equal(ItemBoxTakeoutResponse.WireSize, payload.Length);
        Assert.Equal(result, ItemBoxTakeoutResponse.FromBytes(payload).Result);
    }

    [Fact]
    public void ItemboxOpcodes_MatchTheClient()
    {
        Assert.Equal(0x8782, (int)PacketType.ItemboxItemCreateNotify);
        Assert.Equal(0x1BFE, (int)PacketType.ItemboxItemDeleteNotify);
        Assert.Equal(0xBBE3, (int)PacketType.ItemBoxTakeoutRequest);
        Assert.Equal(0x4135, (int)PacketType.ItemBoxTakeoutResponse);
        Assert.Equal(0x1AAE, (int)PacketType.ExpireItemLimitSelectRRequest);
    }
}
