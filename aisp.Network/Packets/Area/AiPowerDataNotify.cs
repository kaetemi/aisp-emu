using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>recv_aipower_data</c> (0x59C3). July 2009 Area handler <c>0x72ABA0</c> reads two uints
/// then a loop of <see cref="AiPowerCardData.WireSize"/>-byte cards, <c>N ≤ 300</c>, and
/// opens <c>CAipowerWindow</c>. Header is count then a reserved uint.
/// </summary>
public sealed class AiPowerDataNotify(IReadOnlyList<AiPowerCardData> cards) : IOutgoingPacket
{
    public const int HeaderSize = 8;
    public const uint MaxCount = 300;

    public IReadOnlyList<AiPowerCardData> Cards { get; } = cards;

    public byte[] ToBytes()
    {
        if (Cards.Count > MaxCount)
            throw new InvalidOperationException(
                $"recv_aipower_data allows at most {MaxCount} cards."
            );

        var writer = new PacketWriter();
        writer.Write((uint)Cards.Count);
        writer.Write(0u);
        foreach (var card in Cards)
            writer.Write(card.ToBytes());
        return writer.ToBytes();
    }

    public static AiPowerDataNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var count = reader.ReadUInt();
        reader.ReadUInt();
        if (count > MaxCount)
            throw new ArgumentException(
                $"recv_aipower_data count {count} exceeds {MaxCount}.",
                nameof(data)
            );

        var list = new AiPowerCardData[count];
        for (var i = 0; i < count; i++)
            list[i] = AiPowerCardData.FromBytes(reader.ReadBytes(AiPowerCardData.WireSize));
        return new AiPowerDataNotify(list);
    }
}
