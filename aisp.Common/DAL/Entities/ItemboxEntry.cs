namespace aisp.Common.DAL.Entities;

/// <summary>
/// One stack parked in a character's item box: the holding area the capsule machine window shows as
/// ガチャガチャボックス (n/100). Pulls land here instead of in the bag; とりだす moves entries to the bag one
/// at a time. <see cref="Id"/> doubles as the entry id on the wire.
/// </summary>
public class ItemboxEntry
{
    public long Id { get; set; }
    public int CharacterId { get; set; }
    public int ItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Character Character { get; set; } = default!;
}
