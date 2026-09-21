using System.Buffers.Binary;
using aisp.Network.Packets.Msg;

namespace aisp.Network.Tests;

public class AvatarGetCreateInfoTests
{
    [Fact]
    public void September2008_CreateInfo_IsFacesHairColorsThenEquip_ForBothGenders()
    {
        var bytes = new AvatarGetCreateInfoResponse().ToSeptember2008Bytes();
        var reader = new PacketReaderCursor(bytes);

        var maleFaces = reader.ReadBytes(max: 4);
        var maleHair = reader.ReadUInts(max: 4);
        var maleColors = reader.ReadBytes(max: 5);
        var maleEquip = reader.ReadPairs(max: 0x1d);
        var femaleFaces = reader.ReadBytes(max: 4);
        var femaleHair = reader.ReadUInts(max: 4);
        var femaleColors = reader.ReadBytes(max: 5);
        var femaleEquip = reader.ReadPairs(max: 0x1d);

        Assert.Equal(new byte[] { 0, 1, 2, 3 }, maleFaces);
        Assert.Equal(new byte[] { 0, 1, 2, 3 }, femaleFaces);
        Assert.Equal(new uint[] { 10920010, 10920020, 10920030, 10920040 }, maleHair);
        Assert.Equal(new uint[] { 10930010, 10930020, 10930030, 10930040 }, femaleHair);
        Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, maleColors);
        Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, femaleColors);
        Assert.Equal((10100140u, 0u), maleEquip[0]);
        Assert.Equal((10100060u, 0u), femaleEquip[0]);
        Assert.Equal(bytes.Length, reader.Offset);
    }

    [Fact]
    public void July2009_CreateInfo_StillStartsWithBuildUInts()
    {
        var bytes = new AvatarGetCreateInfoResponse().ToBytes();
        Assert.Equal(3u, BinaryPrimitives.ReadUInt32LittleEndian(bytes));
        Assert.Equal(1001021u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)));
    }

    private sealed class PacketReaderCursor(byte[] bytes)
    {
        public int Offset { get; private set; }

        public byte[] ReadBytes(int max)
        {
            var count = ReadUInt();
            Assert.InRange(count, 0u, (uint)max);
            var values = new byte[count];
            for (var i = 0; i < count; i++)
                values[i] = bytes[Offset++];
            return values;
        }

        public uint[] ReadUInts(int max)
        {
            var count = ReadUInt();
            Assert.InRange(count, 0u, (uint)max);
            var values = new uint[count];
            for (var i = 0; i < count; i++)
                values[i] = ReadUInt();
            return values;
        }

        public (uint Id, uint Socket)[] ReadPairs(int max)
        {
            var count = ReadUInt();
            Assert.InRange(count, 0u, (uint)max);
            var values = new (uint, uint)[count];
            for (var i = 0; i < count; i++)
                values[i] = (ReadUInt(), ReadUInt());
            return values;
        }

        private uint ReadUInt()
        {
            var value = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(Offset));
            Offset += 4;
            return value;
        }
    }
}
