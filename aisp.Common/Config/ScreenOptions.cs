namespace aisp.Common.Config;

/// <summary>How the short screen ids that leave the choice to the server play (ScreenAssignments).</summary>
public sealed class ScreenOptions
{
    /// <summary>tw:&lt;channel&gt;: "electron" (Twitch's own player in the off-screen browser, as twe:)
    /// or "streamlink" (decoded through streamlink and ffmpeg, as twl:).</summary>
    public string Twitch { get; set; } = "electron";

    /// <summary>yt:&lt;id&gt;: "electron" (YouTube's own player in the off-screen browser, as yte:)
    /// or "yt-dlp" (resolved by yt-dlp and decoded by ffmpeg, as ytd:).</summary>
    public string YouTube { get; set; } = "electron";
}
