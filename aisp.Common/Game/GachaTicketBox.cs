namespace aisp.Common.Game;

/// <summary>
/// Session-side state of the アイポンボックス: stacks the player dropped in while a machine is open.
/// Nothing leaves the bag in the database until the box is settled, so a dropped connection loses nothing;
/// the client only sees the bag counts go down through item-table notifies.
/// </summary>
public sealed class GachaTicketBox
{
    public required int MachineId { get; init; }
    public required int TicketItemId { get; init; }
    public required int ItemsPerTicket { get; init; }

    /// <summary>Deposited stacks in drop order (item id → quantity).</summary>
    public Dictionary<int, int> Deposits { get; } = [];

    public int Total => Deposits.Values.Sum();

    public int TicketsReady => ItemsPerTicket > 0 ? Total / ItemsPerTicket : 0;
}
