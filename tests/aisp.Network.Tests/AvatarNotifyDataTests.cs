using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class AvatarNotifyDataTests
{
    [Fact]
    public void September2008_Notify_IsResultIdAndTheShortCharacterRecord()
    {
        var character = new CharaData(42, 1_002_011, "eina")
        {
            Visual = new CharaVisual(BloodType.A, 9, 8, 2, 2, 2, 10_930_020),
            Map = new CharacterMapData
            {
                ChannelId = 1,
                MapId = 10_990_100,
                MapSerialId = 10_990_100,
                Movement = new MovementData(1, 2, 3, 0, MovementType.Stopped),
            },
        };
        character.AddEquip(10_100_060, 0);
        var bytes = new AvatarNotifyData(0, new AvatarData(42, character)).ToSeptember2008Bytes();

        var reader = new PacketReader(bytes);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(42u, reader.ReadUInt());
        Assert.Equal(42u, reader.ReadUInt());
        Assert.Equal(1_002_011u, reader.ReadUInt());
        Assert.Equal("eina", reader.ReadFixedString(37));
        var visual = CharaVisual.FromBytes(reader.ReadBytes(19));
        Assert.Equal(2u, visual.Gender);
        Assert.Equal(2, visual.Face);
        Assert.Equal(10_930_020u, visual.Hairstyle);
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

        for (var i = 0; i < 22; i++)
            Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0, reader.Remaining);
        Assert.Equal(426, bytes.Length);
    }
}
