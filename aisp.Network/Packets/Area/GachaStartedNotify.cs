using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>recv_gacha_started</c> (0xCC88). Opens the capsule machine overlay (CGachaWindow, PAS kuzi_window00.xml).
/// There is no client request for this: the server pushes it when the player accesses a machine NPC.
/// <para>
/// VisualId selects the 256x256 package splash drawn in the machine's glass
/// (<c>./interface/package/%08d.dds</c>; 100000 is the stock MoonScape sheet).
/// A machine charges one currency: when NicoPrice is nonzero the client opens it as a P (nicopoint)
/// machine, otherwise as a D (aipoint) machine priced at AiPrice.
/// TicketItemId/TicketsPerPull (the wire's itemserialid/itemnum) enable the チケットを使う path:
/// the client shows how many of that item the bag holds as チケット所有数 and offers a pull for
/// TicketsPerPull of them. Zero hides the ticket controls.
/// </para>
/// </summary>
public sealed class GachaStartedNotify(
    string name,
    uint visualId,
    ulong aiPrice,
    ulong nicoPrice,
    uint ticketItemId = 0,
    ushort ticketsPerPull = 0
) : IOutgoingPacket
{
    /// <summary>The client reads the name into a 0xC1-byte buffer including the terminator.</summary>
    public const int NameMaxBytes = 0xC0;

    public string Name { get; } = name;
    public uint VisualId { get; } = visualId;
    public ulong AiPrice { get; } = aiPrice;
    public ulong NicoPrice { get; } = nicoPrice;
    public uint TicketItemId { get; } = ticketItemId;
    public ushort TicketsPerPull { get; } = ticketsPerPull;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Name, NameMaxBytes);
        writer.Write(VisualId);
        writer.Write(AiPrice);
        writer.Write(NicoPrice);
        writer.Write(TicketItemId);
        writer.Write(TicketsPerPull);
        return writer.ToBytes();
    }

    public static GachaStartedNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GachaStartedNotify(
            reader.ReadString(),
            reader.ReadUInt(),
            reader.ReadULong(),
            reader.ReadULong(),
            reader.ReadUInt(),
            reader.ReadUShort()
        );
    }
}
