using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Test-machine crank. Returns success with no prize so <c>/gacha</c> can open the window
/// without mutating inventory; a later real catalog can fill SerialId/Num.
/// </summary>
public sealed class AreaGachaBuyHandler(ILogger<AreaGachaBuyHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GachaBuyRequest;
    public PacketType ResponseType => PacketType.GachaBuyResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = GachaBuyRequest.FromBytes(payload.Span);
        logger.LogInformation(
            "GachaBuy from character {CharacterId} buyType={BuyType}",
            session.CharacterId,
            request.BuyType
        );
        await session.SendAsync(ResponseType, new GachaBuyResponse(0, 0, 0, 0).ToBytes(), ct);
    }
}

public sealed class AreaGachaEndHandler(ILogger<AreaGachaEndHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GachaEndRequest;
    public PacketType ResponseType => PacketType.GachaEndResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("GachaEnd from character {CharacterId}", session.CharacterId);
        await session.SendAsync(ResponseType, new GachaEndResponse(0).ToBytes(), ct);
        await session.SendAsync(PacketType.GachaEndedNotify, new GachaEndedNotify().ToBytes(), ct);
    }
}

public sealed class AreaGachaTicketExchangeCloseHandler
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GachaTicketExchangeCloseRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    ) => Task.CompletedTask;
}
