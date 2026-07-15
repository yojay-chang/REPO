namespace CMS.API.Models;

/// <summary>
/// FeaturedPromoItem search DTO for <c>POST /api/featured-promo-items/query</c>. The list is a
/// per-training-center weekly scheduler: filter by TrainingCenter and a Monday–Sunday
/// <c>ScheduleOn</c> range (<see cref="ScheduleOnFrom"/>/<see cref="ScheduleOnTo"/>).
/// </summary>
public class FeaturedPromoItemQuery
{
    public short? TrainingCenterPkid { get; set; }
    public DateOnly? ScheduleOnFrom { get; set; }
    public DateOnly? ScheduleOnTo { get; set; }
}
