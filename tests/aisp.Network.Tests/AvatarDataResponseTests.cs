using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;

namespace aisp.Network.Tests;

public class AvatarDataResponseTests
{
    [Fact]
    public void September2008_Record_IsIdNameVisualIslandSlotAnd29ItemIds()
    {
        var packet = new AvatarDataResponse(42, "eina", 1_002_011, 7, 0);
        packet.Visual = new CharaVisual(BloodType.A, 9, 8, 2, 2, 2, 10_930_020);
        packet.AddEquip(10_100_060, 0);
        packet.AddEquip(10_200_090, 0);

        var bytes = packet.ToSeptember2008Bytes();
        var reader = new PacketReader(bytes);
        Assert.Equal(42u, reader.ReadUInt());
        Assert.Equal("eina", reader.ReadString());
        var visual = CharaVisual.FromBytes(reader.ReadBytes(19));
        Assert.Equal(BloodType.A, visual.BloodType);
        Assert.Equal(9, visual.Month);
        Assert.Equal(8, visual.Day);
        Assert.Equal(2u, visual.Gender);
        Assert.Equal(2u, visual.VisualId);
        Assert.Equal(2, visual.Face);
        Assert.Equal(10_930_020u, visual.Hairstyle);
        Assert.Equal(7u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(10_100_060u, reader.ReadUInt());
        Assert.Equal(10_200_090u, reader.ReadUInt());
        for (var i = 0; i < 27; i++)
            Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0, reader.Remaining);
    }
}
