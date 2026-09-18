using System.Text;
using aisp.Common.Game;
using Xunit;

namespace aisp.Common.Tests;

public class SystemNoticeTests
{
    [Fact]
    public void DistId_IsJuly2009SystemNoticeFilter()
    {
        // 2009 0x41fd90: DistId -6 → filter type 5. DistId -5 is type 0 (public chat).
        Assert.Equal(unchecked((uint)-6), SystemNotice.DistId);
        Assert.NotEqual(unchecked((uint)-5), SystemNotice.DistId);
    }

    [Fact]
    public void Messages_KeepEveryMessageUnderTheClientLimit()
    {
        var usage =
            "/screen <source> [extras]. Sources: twitch:<channel> (or tw:), lv<id> (Nico Live), stream:<url>, a web page URL,\n"
            + "fps:N (15/20/25/30/50/60), rolloff:near/far, rolloff:near/far/max/min (gains to fade between, default 1/0), rolloff:x/y/z/near/far, rolloff:x/y/z/near/far/max/min or rolloff:flat, pan to also stereo-pan by bearing.";
        usage = usage + "\n" + usage + "\n" + usage;
        var lines = SystemNotice.Messages(usage).ToList();
        Assert.True(lines.Count >= 2);
        Assert.All(
            lines,
            line => Assert.True(Encoding.UTF8.GetByteCount(line) <= SystemNotice.MaxLineBytes)
        );
        // Nothing lost: the words come back in order.
        Assert.Equal(
            usage.Replace("\n", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries),
            string.Join(' ', lines)
                .Replace("\n", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        );
        // 2009 System/Notice UI cannot host several \n in one payload; each paragraph is its own notify.
        Assert.Equal(
            new[] { "1 alice", "2 bob" },
            SystemNotice.Messages("1 alice\n2 bob").ToList()
        );
    }

    [Fact]
    public void Messages_SplitDefaultMotdIntoOneNotifyPerRule()
    {
        var motd =
            "Welcome to the aisp-emu server project!\n1. Be respectful - Treat other players and staff with respect.\n2. No hate speech or slurs.\n3. No excessive toxicity - Swearing is fine within reason.\n4. No harassing other players\n5. Keep inappropriate content out of public areas.\n6. No spam or disruptive behaviour.\n7. Respect moderator decisions\n8. Use common sense\nIf you see anyone breaking these rules use the '/report' command";
        var lines = SystemNotice.Messages(motd).ToList();
        Assert.True(lines.Count >= 8);
        Assert.All(lines, line => Assert.DoesNotContain('\n', line));
        Assert.All(lines, line => Assert.True(Encoding.UTF8.GetByteCount(line) < 120));
        Assert.StartsWith("Welcome", lines[0]);
        Assert.Contains(lines, line => line.StartsWith("8.", StringComparison.Ordinal));
    }

    [Fact]
    public void Messages_SplitLongWordsOnCharacterBoundaries()
    {
        var url = "https://example.test/" + new string('あ', 80);
        var lines = SystemNotice.Messages(url, 40).ToList();
        Assert.True(lines.Count > 1);
        Assert.All(lines, line => Assert.True(Encoding.UTF8.GetByteCount(line) <= 40));
        Assert.Equal(url, string.Concat(lines));
        Assert.Equal(new[] { "short" }, SystemNotice.Messages("short").ToList());
    }
}
