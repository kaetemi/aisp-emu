namespace aisp.Common.DAL.Entities;

/// <summary>
/// A capsule-toy machine (aiぽん). One machine charges one currency per pull: <see cref="NicoPrice"/> when
/// nonzero (the client opens it as a P machine), otherwise <see cref="AiPrice"/> in D. A machine with
/// neither price and a <see cref="TicketItemId"/> is a ticket machine: the client prices it at
/// <see cref="TicketsPerPull"/> copies of that bag item and shows the owned count as チケット所有数.
/// (Mixing a ticket with a D/P price only shows the count on the 2011 client; it draws no ticket control.)
/// </summary>
public class GachaMachine
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Package splash drawn in the glass: <c>./interface/package/%08d.dds</c> on the client.</summary>
    public long VisualId { get; set; }
    public long AiPrice { get; set; }
    public long NicoPrice { get; set; }
    public int? TicketItemId { get; set; }
    public int TicketsPerPull { get; set; } = 1;
    public bool IsEnabled { get; set; } = true;

    public ICollection<GachaPrize> Prizes { get; set; } = new List<GachaPrize>();
    public ICollection<Npc> Npcs { get; set; } = new List<Npc>();
}
