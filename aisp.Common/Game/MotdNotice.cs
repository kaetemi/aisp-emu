using aisp.Common.Config;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

public static class MotdNotice
{
    public static Task TrySendPendingAsync(
        IPlayerSession session,
        SharedState state,
        MotdOptions? options,
        ILogger logger,
        CancellationToken ct = default
    )
    {
        if (!session.NeedsMotd)
            return Task.CompletedTask;

        session.NeedsMotd = false;
        // July 2009 has no System / Notice chat UI. TalkForward MOTD (DistId -5 as public
        // FromId=0, or DistId -6/-7 as type 5/6) aborts at 0x42642d on HUD load.
        logger.LogDebug(
            "Skipping MOTD for user {UserId}: July 2009 has no System/Notice UI",
            session.UserId
        );
        _ = (state, options, ct);
        return Task.CompletedTask;
    }
}
