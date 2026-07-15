namespace CMS.API.Models;

/// <summary>FeaturedPromoItem write DTO. <see cref="Pkid"/> is used for UPDATE; ignored on INSERT (IDENTITY).</summary>
public class FeaturedPromoItemRequest
{
    public int Pkid { get; set; }
    public DateOnly ScheduleOn { get; set; }
    public short TrainingCenterPkid { get; set; }
    public byte Slot { get; set; }
    public int PromotionPkid { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
