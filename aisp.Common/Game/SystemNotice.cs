using System.Text;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Game;

public static class SystemNotice
{
    /// <summary>
    /// DistId for the System / Notice chat filter on the July 2009 client.
    /// 2009 <c>0x41fd90</c> maps DistId -6 → filter type 5 (inverse <c>0x41fe26</c> returns -6).
    /// DistId -5 is a hole in that table (type 0, same as public chat). 2011 remapped the
    /// same filter type onto DistId -5 (<c>sub_428B10</c>). Wine still aborts at HUD load if
    /// MOTD is enabled (DistId -6 and -7 both abort); keep Motd disabled until the notice
    /// UI path is fully reversed. Split each source paragraph into its own notify.
    /// </summary>
    public const uint DistId = unchecked((uint)-6);

    /// <summary>
    /// The client's recv_talk_forward reads the message into a char[0x181]: it scans the first
    /// 385 bytes for a NUL and treats a longer string as a malformed RPC, which drops the
    /// session. The same cap applies to the client's own send_talk_post. Messages therefore
    /// carry at most this many bytes of text before the CRLF and NUL, split into several
    /// notifies when longer.
    /// </summary>
    public const int MaxLineBytes = 360;

    public static async Task SendAsync(
        IPlayerSession session,
        string text,
        CancellationToken ct = default
    )
    {
        foreach (var message in Messages(text))
            await session.SendAsync(
                PacketType.TalkForwardNotify,
                new TalkForwardNotify(0, DistId, $"{message}\r\n", 0).ToBytes(),
                ct
            );
    }

    /// <summary>
    /// Splits a notice into messages of at most <see cref="MaxLineBytes"/> UTF-8 bytes. Each
    /// source paragraph is its own notify: the 2009 System/Notice UI aborts at <c>0x42642d</c>
    /// when a single DistId -6 payload embeds several <c>\n</c> lines (the default MOTD was
    /// 372 bytes / ~9 rules). Over-long paragraphs wrap on spaces, or inside a word only when
    /// the word itself is too long.
    /// </summary>
    public static IEnumerable<string> Messages(string text, int maxBytes = MaxLineBytes)
    {
        var message = new StringBuilder();
        foreach (var (line, newParagraph) in Wrap(text, maxBytes))
        {
            var candidate = message.Length == 0 ? line : message + " " + line;
            if (
                message.Length > 0
                && (newParagraph || Encoding.UTF8.GetByteCount(candidate) > maxBytes)
            )
            {
                yield return message.ToString();
                message.Clear().Append(line);
            }
            else
            {
                message.Clear().Append(candidate);
            }
        }
        if (message.Length > 0)
            yield return message.ToString();
    }

    private static IEnumerable<(string Line, bool NewParagraph)> Wrap(string text, int maxBytes)
    {
        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = new StringBuilder();
            var first = true;
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var piece in Pieces(word, maxBytes))
                {
                    var candidate = line.Length == 0 ? piece : line + " " + piece;
                    if (line.Length > 0 && Encoding.UTF8.GetByteCount(candidate) > maxBytes)
                    {
                        yield return (line.ToString(), first);
                        first = false;
                        line.Clear().Append(piece);
                    }
                    else
                    {
                        line.Clear().Append(candidate);
                    }
                }
            }
            if (line.Length > 0)
                yield return (line.ToString(), first);
        }
    }

    private static IEnumerable<string> Pieces(string word, int maxBytes)
    {
        if (Encoding.UTF8.GetByteCount(word) <= maxBytes)
        {
            yield return word;
            yield break;
        }
        var piece = new StringBuilder();
        foreach (var rune in word.EnumerateRunes())
        {
            if (piece.Length > 0 && Encoding.UTF8.GetByteCount(piece + rune.ToString()) > maxBytes)
            {
                yield return piece.ToString();
                piece.Clear();
            }
            piece.Append(rune.ToString());
        }
        if (piece.Length > 0)
            yield return piece.ToString();
    }
}
