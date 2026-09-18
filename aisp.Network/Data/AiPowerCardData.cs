namespace aisp.Network.Data;

/// <summary>
/// One <c>recv_aipower_data</c> card. July 2009 parser <c>0x71A9D0</c> consumes exactly
/// <see cref="WireSize"/> bytes: a ushort, two C strings, three ushorts, five more C strings,
/// then a 601-byte tail the UI reads for gauges / hearts / the 3D visual.
/// </summary>
public sealed class AiPowerCardData
{
    public const int WireSize = 0x4C0;
    public const int NameBytes = 0x5B;
    public const int CaptionBytes = 0x3D;
    public const int ProfileFieldCount = 5;
    public const int ProfileFieldBytes = 0x5B;
    public const int TrailingBytes = 0x259;

    public ushort Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public ushort Param0 { get; set; }
    public ushort Param1 { get; set; }
    public ushort Param2 { get; set; }
    public string[] ProfileFields { get; set; } = ["", "", "", "", ""];
    public byte[] Trailing { get; set; } = new byte[TrailingBytes];

    public byte[] ToBytes()
    {
        if (ProfileFields.Length != ProfileFieldCount)
            throw new InvalidOperationException(
                $"AiPowerCardData must contain exactly {ProfileFieldCount} profile fields."
            );
        if (Trailing.Length != TrailingBytes)
            throw new InvalidOperationException(
                $"AiPowerCardData trailing blob must be {TrailingBytes} bytes."
            );

        var writer = new PacketWriter();
        writer.Write(Id);
        writer.WriteFixedStringNulTerminated(Name, NameBytes);
        writer.WriteFixedStringNulTerminated(Caption, CaptionBytes);
        writer.Write(Param0);
        writer.Write(Param1);
        writer.Write(Param2);
        foreach (var field in ProfileFields)
            writer.WriteFixedStringNulTerminated(field, ProfileFieldBytes);
        writer.Write(Trailing);
        return writer.ToBytes();
    }

    public static AiPowerCardData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException(
                $"AiPowerCardData requires at least {WireSize} bytes.",
                nameof(data)
            );

        var reader = new PacketReader(data);
        var fields = new string[ProfileFieldCount];
        var card = new AiPowerCardData
        {
            Id = reader.ReadUShort(),
            Name = reader.ReadFixedString(NameBytes),
            Caption = reader.ReadFixedString(CaptionBytes),
            Param0 = reader.ReadUShort(),
            Param1 = reader.ReadUShort(),
            Param2 = reader.ReadUShort(),
        };
        for (var i = 0; i < ProfileFieldCount; i++)
            fields[i] = reader.ReadFixedString(ProfileFieldBytes);
        card.ProfileFields = fields;
        card.Trailing = reader.ReadBytes(TrailingBytes).ToArray();
        return card;
    }
}
