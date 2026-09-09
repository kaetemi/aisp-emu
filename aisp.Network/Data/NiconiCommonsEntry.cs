using aisp.Network;

namespace aisp.Network.Data;

/// <summary>
/// One title in <c>recv_get_niconi_commons_base_list_r</c>: an entry of the drama notebook's
/// title carousel, one per figure box, requested together with the figure list. 113 bytes on
/// the wire (the client's parser at <c>0x79B140</c>; 0x74 apart in memory).
/// </summary>
public sealed class NiconiCommonsEntry
{
    public const int WireSize = 113;
    public const int NameBytes = 96;

    /// <summary>The box this title stands for (1, 2, 1000, …), matching the figures' BoxId.</summary>
    public uint Id { get; init; }
    public string Name { get; init; } = "";
    public bool Available { get; init; } = true;

    public void Write(PacketWriter writer)
    {
        writer.Write(Id);
        // Three words the client copies into its own record unread by the parser; a value
        // other than zero in the second or third crashed the login when tried.
        writer.Write(0u);
        writer.Write(0u);
        writer.WriteFixedStringNulTerminated(Name, NameBytes, "Shift_JIS");
        writer.Write(Available ? (byte)1 : (byte)0);
        writer.Write(0u);
    }
}
