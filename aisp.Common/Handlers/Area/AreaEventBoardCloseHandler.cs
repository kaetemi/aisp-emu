using aisp.Common.Game;
using aisp.Network;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>Client closed the board chrome. No reply on the wire.</summary>
public sealed class AreaEventBoardCloseHandler(ILogger<AreaEventBoardCloseHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventBoardCloseRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("EventBoardClose from character {CharacterId}", session.CharacterId);
        return Task.CompletedTask;
    }
}
