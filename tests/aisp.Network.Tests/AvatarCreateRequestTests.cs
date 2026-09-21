using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;

namespace aisp.Network.Tests;

public class AvatarCreateRequestTests
{
    [Fact]
    public void September2008_Create_OmitsModelId_AndKeepsThe19ByteVisual()
    {
        // Live 2026-09-21: eina, 9/8, blood A, female, face 2, long hair 10930020, slot 0.
        byte[] captured =
        [
            0x65,
            0x69,
            0x6E,
            0x61,
            0x00,
            0x01,
            0x00,
            0x00,
            0x00,
            0x09,
            0x08,
            0x02,
            0x00,
            0x00,
            0x00,
            0x02,
            0x00,
            0x00,
            0x00,
            0x02,
            0x64,
            0xC7,
            0xA6,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
        ];

        var request = AvatarCreateRequest.FromBytes(captured);

        Assert.Equal("eina", request.AvatarName);
        Assert.Equal(1002011u, request.modelId);
        Assert.Equal(BloodType.A, request.visual.BloodType);
        Assert.Equal(9, request.visual.Month);
        Assert.Equal(8, request.visual.Day);
        Assert.Equal(2u, request.visual.Gender);
        Assert.Equal(2, request.visual.Face);
        Assert.Equal(10930020u, request.visual.Hairstyle);
        Assert.Equal(0u, request.slotId);
    }

    [Fact]
    public void July2009_Create_StillReadsModelIdBeforeTheVisual()
    {
        var writer = new PacketWriter();
        writer.Write("kaetemi");
        writer.Write(1002011u);
        writer.Write(new CharaVisual(BloodType.A, 9, 21, 2, 1002011, 1, 10930010).ToBytes());
        writer.Write(0u);

        var request = AvatarCreateRequest.FromBytes(writer.ToBytes());

        Assert.Equal("kaetemi", request.AvatarName);
        Assert.Equal(1002011u, request.modelId);
        Assert.Equal(10930010u, request.visual.Hairstyle);
        Assert.Equal(0u, request.slotId);
    }
}
