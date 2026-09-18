using System.Text;

namespace aisp.Network.Data;

/// <summary>
/// One <c>recv_aipower_data</c> card. July 2009 parser <c>0x71A9D0</c> consumes exactly
/// <see cref="WireSize"/> bytes. <c>CAipowerCharaSheet</c> apply is <c>0x5d2320</c>:
/// <see cref="Id"/> loads <c>./aipower/%05d.dds</c> (str_table 100,940,1); missing file hides the sheet.
/// <see cref="Param0"/> / <see cref="Param1"/> are 0–100 gauge percents (heart pip = Param0/20).
/// The 601-byte tail is a UTF-8 C string (code page 65001); <c>&lt;BR&gt;</c> splits the five
/// プロフィール rows. <see cref="ProfileFields"/> are balloon-line candidates (one is shown).
/// </summary>
public sealed class AiPowerCardData
{
    public const int WireSize = 0x4C0;
    public const int NameBytes = 0x5B;
    public const int CaptionBytes = 0x3D;
    public const int ProfileFieldCount = 5;
    public const int ProfileFieldBytes = 0x5B;
    public const int TrailingBytes = 0x259;
    public const string BalloonLineBreak = "<BR>";

    /// <summary>D.C.II 月島小恋 portrait in <c>aipower.hed</c>.</summary>
    public const ushort VisualKomari = 10000;

    /// <summary>D.C.II 白河ななか portrait in <c>aipower.hed</c>.</summary>
    public const ushort VisualNanaka = 10001;

    /// <summary>D.C.II 朝倉由夢 portrait in <c>aipower.hed</c>.</summary>
    public const ushort VisualYume = 10002;

    public static readonly ushort[] CatalogVisualIds = [VisualKomari, VisualNanaka, VisualYume];

    public ushort Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;

    /// <summary>Upper cheer gauge 0–100. Heart widget = PAS 380 + value/20. ≥100 shows MAX.</summary>
    public ushort Param0 { get; set; }

    /// <summary>Lower cheer gauge 0–100.</summary>
    public ushort Param1 { get; set; }

    /// <summary>Added into the upper gauge when a cheer tick overflows the lower gauge.</summary>
    public ushort Param2 { get; set; }

    /// <summary>Up to five balloon / 台詞 candidates. Apply <c>0x5d1bd0</c> picks one at random.</summary>
    public string[] ProfileFields { get; set; } = ["", "", "", "", ""];
    public byte[] Trailing { get; set; } = new byte[TrailingBytes];

    /// <summary>UTF-8 プロフィール rows at the start of <see cref="Trailing"/>, joined with <see cref="BalloonLineBreak"/> (PAS 190–194).</summary>
    public string BalloonText
    {
        get
        {
            var end = Array.IndexOf(Trailing, (byte)0);
            if (end < 0)
                end = Trailing.Length;
            return end == 0 ? string.Empty : Encoding.UTF8.GetString(Trailing, 0, end);
        }
        set => SetBalloon(value);
    }

    public void SetBalloon(string text)
    {
        if (Trailing.Length != TrailingBytes)
            Trailing = new byte[TrailingBytes];
        else
            Array.Clear(Trailing);
        if (string.IsNullOrEmpty(text))
            return;
        var bytes = Encoding.UTF8.GetBytes(text);
        var n = Math.Min(bytes.Length, TrailingBytes - 1);
        bytes.AsSpan(0, n).CopyTo(Trailing);
    }

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
