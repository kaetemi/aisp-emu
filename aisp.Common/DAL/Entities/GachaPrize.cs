namespace aisp.Common.DAL.Entities;

/// <summary>One possible capsule: the pull picks an enabled prize with probability Weight / sum of weights.</summary>
public class GachaPrize
{
    public int Id { get; set; }
    public int GachaMachineId { get; set; }
    public int ItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public int Weight { get; set; } = 1;

    /// <summary>Forwarded to the client's result window as the hit class (recv_gacha_buy_r hit_type).</summary>
    public int HitType { get; set; } = 1;
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;

    public GachaMachine Machine { get; set; } = default!;
}
