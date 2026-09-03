using aisp.Common.Game;

namespace aisp.Common.Tests;

public sealed class ScreenAssignmentsTests
{
    [Fact]
    public void RoomTv_PlaysATypedTwitchChannel_OtherwiseTheMapAssignment()
    {
        var assignments = new ScreenAssignments();
        assignments.Set(10990100, " twitch:someone ");

        // A Twitch channel typed into the TV wins, whatever the map says; tw: is the short form.
        Assert.Equal("twitch:me", assignments.Resolve("room-tv", "twitch:me", 10990100));
        Assert.Equal(
            "twitch:averylongchannelname",
            assignments.Resolve("room-tv", " tw:averylongchannelname ", null)
        );
        // Typed URLs and streams are not honoured: they would make every viewer fetch them.
        Assert.Equal(
            "twitch:someone",
            assignments.Resolve("room-tv", "stream:https://x/y.m3u8", 10990100)
        );
        Assert.Null(assignments.Resolve("room-tv", "https://example.test/", null));
        // A plain Nico id is not something the hook can play: fall back to the map, then to nothing.
        Assert.Equal("twitch:someone", assignments.Resolve("room-tv", "sm9", 10990100));
        Assert.Null(assignments.Resolve("room-tv", "sm9", 19001003));
        Assert.Null(assignments.Resolve("room-tv", "sm9", null));
        // testscreen always shows the diagnostic page, even on an assigned map.
        Assert.Null(assignments.Resolve("room-tv", "testscreen", 10990100));
        assignments.Set(19001003, "TestScreen");
        Assert.Null(assignments.Resolve("live-watch", null, 19001003));
    }

    [Fact]
    public void TownScreens_FollowTheMapAssignment_UntilCleared()
    {
        var assignments = new ScreenAssignments();
        Assert.Null(assignments.Resolve("channel-screen", null, 10990100));

        assignments.Set(10990100, "tw:someone");
        Assert.Equal("twitch:someone", assignments.Resolve("channel-screen", null, 10990100));
        Assert.Equal("twitch:someone", assignments.Resolve("live-watch", null, 10990100));
        Assert.Equal("twitch:someone", assignments.Get(10990100));

        Assert.True(assignments.Clear(10990100));
        Assert.False(assignments.Clear(10990100));
        Assert.Null(assignments.Resolve("live-watch", null, 10990100));
    }

    [Fact]
    public void Sources_AreStreamsOrPages()
    {
        Assert.True(ScreenAssignments.IsStreamSource("twitch:yueri"));
        Assert.True(ScreenAssignments.IsStreamSource("stream:http://host/a.mp4"));
        Assert.False(ScreenAssignments.IsStreamSource("HTTP://host/page"));
        Assert.True(ScreenAssignments.IsPageUrl("HTTP://host/page"));
        Assert.False(ScreenAssignments.IsPageUrl("twitch:yueri"));
        Assert.True(ScreenAssignments.IsValidSource("stream:https://x/y.m3u8"));
        Assert.True(ScreenAssignments.IsValidSource("testscreen"));
        Assert.True(ScreenAssignments.IsValidSource("calibrate"));
        // A second word is a banner page for the Stage wall; it has to be a page.
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri https://example.test/banner"));
        Assert.True(ScreenAssignments.IsValidSource("blank https://example.test/banner"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri twitch:other"));
        Assert.Equal(
            "twitch:yueri https://x/",
            ScreenAssignments.Normalize(" tw:yueri  https://x/ ")
        );
        Assert.False(ScreenAssignments.IsValidSource("sm9"));
        Assert.False(ScreenAssignments.IsValidSource(null));
    }
}
