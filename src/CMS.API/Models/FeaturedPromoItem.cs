namespace CMS.API.Models;

/// <summary>
/// FeaturedPromoItem 上稿作業 — response model. One scheduled promotion slot for a training
/// center on a given day. <see cref="PromotionPkid"/> carries the JOINed <see cref="PromoCode"/>
/// (Promotion2) and <see cref="TrainingCenterPkid"/> the JOINed <see cref="TrainingCenterName"/>
/// as read-only display columns.
/// </summary>
public class FeaturedPromoItem
{
    public int Pkid { get; set; }
    public DateOnly ScheduleOn { get; set; }
    public short TrainingCenterPkid { get; set; }
    public byte Slot { get; set; }
    public int PromotionPkid { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // JOINed display labels (read-only, not written).
    public string? PromoCode { get; set; }
    public string? TrainingCenterName { get; set; }
}
