using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game.ServerScripts;

/// <summary>
/// Hotaru in front of the 外神田 building. Before the player has registered a home island she plays her
/// tutorial introduction (the client script); afterwards she runs the aiぽん recycle: a yes/no, then the
/// standalone ticket box (<c>recv_gachaticket_exchange_open</c>) for the machine the NPC is linked to.
/// Deposits are settled by the box close handler; she then says how many tickets came out.
/// </summary>
public sealed class HotaruServerScript(
    ICharacterRepository characterRepository,
    IGachaRepository gachaRepository,
    GachaTicketExchange ticketExchange,
    ServerScriptSession serverScriptSession,
    ITextLocaliser localiser,
    ILogger<HotaruServerScript> logger
) : IServerScript
{
    private const uint EventFailure = 1;
    private const string AwaitingChoiceStep = "AwaitingChoice";
    private const string AwaitingBoxCloseStep = "AwaitingBoxClose";
    private const string AwaitingMessageSyncStep = "AwaitingMessageSync";
    private const byte RecycleChoice = 0;

    public string EventKey => ServerEvents.Keys.Hotaru;
    public EventCompletionPolicy CompletionPolicy => EventCompletionPolicy.Replayable;

    public async Task StartAsync(
        IPlayerSession session,
        ServerScriptContext context,
        CancellationToken ct = default
    )
    {
        var character = await characterRepository.GetByIdAsync((int)session.CharacterId, ct);
        if (character is null)
        {
            await serverScriptSession.AbortAsync(session, EventFailure, ct);
            return;
        }

        session.Character = character;
        if (character.HomeIslandId == 0)
        {
            // Hand the already-started event over to the tutorial client script; the regular
            // client-script completion path ends it.
            session.ServerScriptState = null;
            session.ActiveEventKey = ScriptedEvents.Keys.IntroductionHotaru0;
            session.ActiveEventKind = NpcEventKind.ClientScript;
            session.ActiveEventCompletionPolicy = EventCompletionPolicy.Replayable;
            await session.SendAsync(
                PacketType.EventScriptPlayNotify,
                new EventScriptPlayNotify(
                    ScriptedEvents.GetScriptLabel(ScriptedEvents.Keys.IntroductionHotaru0)
                ).ToBytes(),
                ct
            );
            return;
        }

        var machine = context.Npc.GachaMachineId is int machineId
            ? await gachaRepository.GetEnabledMachineAsync(machineId, ct)
            : null;
        if (machine is null || machine.RecycleItemsPerTicket <= 0 || machine.TicketItemId is null)
        {
            await SayAndSyncAsync(
                session,
                context.Npc,
                localiser.Get(session, L.Script.Hotaru.RecycleUnavailable),
                ct
            );
            return;
        }

        session.ServerScriptState!.Step = AwaitingChoiceStep;
        session.ServerScriptState.Data["machineId"] = machine.Id;
        session.ServerScriptState.Data["npcObjectId"] = checked((uint)context.Npc.NpcObjectId);
        await session.SendAsync(
            PacketType.EventSelectInitNotify,
            new EventSelectInitNotify { SelectType = EventSelectType.Popup }.ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.EventSelectPushNotify,
            new EventSelectPushNotify
            {
                SelectName = localiser.Get(session, L.Script.Hotaru.RecycleYes),
            }.ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.EventSelectPushNotify,
            new EventSelectPushNotify
            {
                SelectName = localiser.Get(session, L.Script.Hotaru.RecycleNo),
            }.ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.EventSelectExecNotify,
            new EventSelectExecNotify
            {
                Text = localiser.Get(
                    session,
                    L.Script.Hotaru.RecyclePrompt,
                    machine.RecycleItemsPerTicket
                ),
            }.ToBytes(),
            ct
        );
    }

    public async Task<bool> TryHandlePacketAsync(
        PacketType packetType,
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var state = session.ServerScriptState;
        if (state is null || !string.Equals(state.EventKey, EventKey, StringComparison.Ordinal))
            return false;

        switch (state.Step)
        {
            case AwaitingChoiceStep when packetType == PacketType.EventSelectExecRRequest:
            {
                var request = EventSelectExecRRequest.FromBytes(payload.Span);
                if (request.Result != 0 || request.SelectNo != RecycleChoice)
                {
                    await serverScriptSession.CompleteAsync(
                        session,
                        request.Result,
                        markComplete: false,
                        ct
                    );
                    return true;
                }

                var machineId = (int)state.Data["machineId"];
                session.ActiveGachaMachineId = machineId;
                var box = await ticketExchange.OpenAsync(session, ct);
                if (box is null)
                {
                    session.ActiveGachaMachineId = null;
                    await serverScriptSession.CompleteAsync(
                        session,
                        EventFailure,
                        markComplete: false,
                        ct
                    );
                    return true;
                }

                state.Step = AwaitingBoxCloseStep;
                logger.LogInformation(
                    "Hotaru opened the ticket box for character {CharacterId} (machine {MachineId}, {Quota} per ticket)",
                    session.CharacterId,
                    machineId,
                    box.ItemsPerTicket
                );
                await session.SendAsync(
                    PacketType.GachaTicketExchangeOpenNotify,
                    new GachaTicketExchangeOpenNotify(checked((uint)box.ItemsPerTicket)).ToBytes(),
                    ct
                );
                return true;
            }

            case AwaitingBoxCloseStep when packetType == PacketType.GachaTicketExchangeCloseRequest:
            {
                // The close handler has already settled the box; report what came out of it.
                var tickets = (int)state.Data.GetValueOrDefault("tickets", 0);
                session.ActiveGachaMachineId = null;
                var npcObjectId = (uint)state.Data.GetValueOrDefault("npcObjectId", 0u);
                var text =
                    tickets > 0
                        ? localiser.Get(session, L.Script.Hotaru.RecycleDone, tickets)
                        : localiser.Get(session, L.Script.Hotaru.RecycleNothing);
                await SayAndSyncAsync(session, npcObjectId, text, ct);
                return true;
            }

            case AwaitingMessageSyncStep when packetType == PacketType.EventSyncRRequest:
            {
                var request = EventSyncRRequest.FromBytes(payload.Span);
                await serverScriptSession.CompleteAsync(
                    session,
                    request.Result,
                    markComplete: false,
                    ct
                );
                return true;
            }
        }

        return false;
    }

    /// <summary>Called by the box close handler so the follow-up line can name the payout.</summary>
    public static void RecordSettlement(IPlayerSession session, int tickets)
    {
        if (
            session.ServerScriptState is { } state
            && state.EventKey == ServerEvents.Keys.Hotaru
            && state.Step == AwaitingBoxCloseStep
        )
            state.Data["tickets"] = tickets;
    }

    private Task SayAndSyncAsync(IPlayerSession session, Npc npc, string text, CancellationToken ct)
    {
        session.ServerScriptState!.Data["npcObjectId"] = checked((uint)npc.NpcObjectId);
        return SayAndSyncAsync(session, checked((uint)npc.NpcObjectId), text, ct);
    }

    private async Task SayAndSyncAsync(
        IPlayerSession session,
        uint npcObjectId,
        string text,
        CancellationToken ct
    )
    {
        session.ServerScriptState!.Step = AwaitingMessageSyncStep;
        var name = localiser.Get(session, L.Npc.Name(npcObjectId));
        await session.SendAsync(
            PacketType.EventMessageNotify,
            new EventMessageNotify(npcObjectId, name, text).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.EventMessageCloseNotify,
            new EventMessageCloseNotify().ToBytes(),
            ct
        );
        await session.SendAsync(PacketType.EventSyncNotify, new EventSyncNotify().ToBytes(), ct);
    }
}
