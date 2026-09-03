using System.Collections.Concurrent;

namespace aisp.Common.Game;

/// <summary>
/// What the in-game screens on a map should play. The client's town displays (the Akihabara
/// screen, the Stage billboard) and room TVs are IE controls that load a page from the emulator
/// (see ScreenEndpointsExtensions); the page tells the launcher's hook what to stream by
/// publishing a source in its title, and this is where that source comes from. One source per
/// map for now, set with the /screen chat command; a movie id typed into a room TV that already
/// names a stream (twitch:&lt;channel&gt;, stream:&lt;url&gt;) or a page (an http(s) URL) is used as is.
/// </summary>
public sealed class ScreenAssignments
{
    private readonly ConcurrentDictionary<uint, string> _byMap = new();

    public void Set(uint mapId, string source) => _byMap[mapId] = Normalize(source);

    /// <summary>An empty main area; used with a banner page on the Stage wall.</summary>
    public const string Blank = "blank";

    /// <summary>
    /// Canonical form of a source: trimmed, with the short tw: alias expanded to twitch: (the
    /// TV's movie id box is too short for long channel names behind the full prefix). A source
    /// is "&lt;main&gt; [&lt;banner page url&gt;]": the second word is a web page for the Stage
    /// wall's banner, which room TVs ignore.
    /// </summary>
    public static string Normalize(string source)
    {
        var words = source.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return "";
        var main = words[0].StartsWith("tw:", StringComparison.OrdinalIgnoreCase)
            ? "twitch:" + words[0][3..]
            : words[0];
        return words.Length > 1 ? main + " " + words[1] : main;
    }

    private static string MainOf(string source) => Normalize(source).Split(' ')[0];

    private static string? BannerOf(string source)
    {
        var words = Normalize(source).Split(' ');
        return words.Length > 1 ? words[1] : null;
    }

    public bool Clear(uint mapId) => _byMap.TryRemove(mapId, out _);

    public string? Get(uint mapId) => _byMap.TryGetValue(mapId, out var source) ? source : null;

    /// <summary>The diagnostic page itself, whatever the map is set to.</summary>
    public const string TestScreen = "testscreen";

    /// <summary>The diagnostic page with a fine coordinate grid, for measuring screen panels.</summary>
    public const string Calibrate = "calibrate";

    /// <summary>
    /// Sources the launcher hook decodes itself: twitch:&lt;channel&gt; (tw: for short) through
    /// streamlink, or stream:&lt;url&gt; (an MP4, HLS playlist, anything ffmpeg opens).
    /// </summary>
    public static bool IsStreamSource(string? source) =>
        source is not null
        && (
            MainOf(source).StartsWith("twitch:", StringComparison.OrdinalIgnoreCase)
            || MainOf(source).StartsWith("stream:", StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>A web page the screen shows as is, framed inside the screen page.</summary>
    public static bool IsPageUrl(string? source) =>
        source is not null
        && (
            MainOf(source).StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || MainOf(source).StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>
    /// What /screen accepts: a stream, a page, blank, the test page, the calibration aids; with
    /// an optional second word naming a web page for the banner.
    /// </summary>
    public static bool IsValidSource(string? source)
    {
        if (source is null)
            return false;
        var main = MainOf(source);
        var banner = BannerOf(source);
        if (banner is not null && !IsPageUrl(banner))
            return false;
        return IsStreamSource(main)
            || IsPageUrl(main)
            || string.Equals(main, Blank, StringComparison.OrdinalIgnoreCase)
            || string.Equals(main, TestScreen, StringComparison.OrdinalIgnoreCase)
            || string.Equals(main, Calibrate, StringComparison.OrdinalIgnoreCase)
            // c:x1,y1:x2,y2[:x3,y3:x4,y4]... draws those rectangles on the page, for measuring.
            || main.StartsWith("c:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The source a screen page should publish, given the route the hook sent the client to,
    /// the movie id (room TVs only) and the map the hook read from the client. Null means the
    /// page shows itself. A typed movie id only reaches this for Twitch channels and the test
    /// page: arbitrary URLs and streams make every viewer's client fetch them, so those come
    /// from /screen alone.
    /// </summary>
    public string? Resolve(string route, string? movieId, uint? mapId)
    {
        if (route == "room-tv" && movieId is not null)
        {
            var typed = MainOf(movieId);
            if (string.Equals(typed, TestScreen, StringComparison.OrdinalIgnoreCase))
                return null;
            if (typed.StartsWith("twitch:", StringComparison.OrdinalIgnoreCase))
                return typed;
        }
        var assigned = mapId is { } map ? Get(map) : null;
        return string.Equals(assigned, TestScreen, StringComparison.OrdinalIgnoreCase)
            ? null
            : assigned;
    }
}
