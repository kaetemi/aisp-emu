using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Tests;

public class NpcNotifyDataTests
{
    [Fact]
    public void September2008_NpcNotify_IsResultObjectIdAndTheShortCharacterRecord()
    {
        var character = new CharaData(99, 1_001_011, "guide")
        {
            Map = new CharacterMapData
            {
                ChannelId = 1,
                MapId = 10_990_100,
                MapSerialId = 10_990_100,
                Movement = new MovementData(1, 2, 3, 0, MovementType.Stopped),
            },
        };
        character.AddEquip(10_100_060, 0);
        var bytes = new NpcNotifyData(0, 99, character).ToSeptember2008Bytes();

        var reader = new PacketReader(bytes);
        Assert.Equal(338, bytes.Length);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(99u, reader.ReadUInt());
        Assert.Equal(99u, reader.ReadUInt());
        Assert.Equal(1_001_011u, reader.ReadUInt());
        Assert.Equal("guide", reader.ReadFixedString(37));
        _ = reader.ReadBytes(19);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(10_990_100u, reader.ReadUInt());
        Assert.Equal(10_990_100u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        _ = reader.ReadBytes(14);
        Assert.Equal(10_100_060u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        for (var i = 0; i < 28; i++)
        {
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
        }

        Assert.Equal(0, reader.Remaining);
    }
}
