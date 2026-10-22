namespace aisp.Common.DAL.Entities;

/// <summary>
/// A capsule-toy machine (aiぽん). One machine charges one currency per pull: <see cref="NicoPrice"/> when
/// nonzero (the client opens it as a P machine), otherwise <see cref="AiPrice"/> in D. Optionally a bag
/// item doubles as a ticket so the client's チケットを使う button can pay <see cref="TicketsPerPull"/>
/// copies of <see cref="TicketItemId"/> instead.
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
