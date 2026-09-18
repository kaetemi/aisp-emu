using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class AiPowerDataTests
{
    [Fact]
    public void EmptyNotify_IsEightByteHeader()
    {
        var bytes = new AiPowerDataNotify([]).ToBytes();
        Assert.Equal(AiPowerDataNotify.HeaderSize, bytes.Length);

        var parsed = AiPowerDataNotify.FromBytes(bytes);
        Assert.Empty(parsed.Cards);
    }

    [Fact]
    public void Card_RoundTripsNameCaptionAndParams()
    {
        var card = new AiPowerCardData
        {
            Id = 7,
            Name = "Kaetemi",
            Caption = "開発中",
            Param0 = 3,
            Param1 = 6,
            Param2 = 9,
            ProfileFields = ["a", "b", "c", "d", "e"],
        };
        card.Trailing[0] = 0xAB;

        var bytes = card.ToBytes();
        Assert.Equal(AiPowerCardData.WireSize, bytes.Length);

        var parsed = AiPowerCardData.FromBytes(bytes);
        Assert.Equal(7, parsed.Id);
        Assert.Equal("Kaetemi", parsed.Name);
        Assert.Equal("開発中", parsed.Caption);
        Assert.Equal(3, parsed.Param0);
        Assert.Equal(6, parsed.Param1);
        Assert.Equal(9, parsed.Param2);
        Assert.Equal(["a", "b", "c", "d", "e"], parsed.ProfileFields);
        Assert.Equal(0xAB, parsed.Trailing[0]);
    }

    [Fact]
    public void Notify_WritesCountThenCards()
    {
        var card = new AiPowerCardData { Id = 1, Name = "One" };
        var bytes = new AiPowerDataNotify([card]).ToBytes();
        Assert.Equal(AiPowerDataNotify.HeaderSize + AiPowerCardData.WireSize, bytes.Length);

        var reader = new PacketReader(bytes);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        var parsed = AiPowerCardData.FromBytes(reader.ReadBytes(AiPowerCardData.WireSize));
        Assert.Equal("One", parsed.Name);
    }

    [Fact]
    public void Notify_RejectsMoreThanThreeHundredCards()
    {
        var cards = Enumerable
            .Range(0, (int)AiPowerDataNotify.MaxCount + 1)
            .Select(_ => new AiPowerCardData())
            .ToArray();
        Assert.Throws<InvalidOperationException>(() => new AiPowerDataNotify(cards).ToBytes());
    }
}
