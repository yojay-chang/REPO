namespace CMS.API.Models;

/// <summary>
/// CourseGroup 課程群組 — response model.
/// PK is <see cref="Pkid"/> (smallint), a <b>system-assigned</b> IDENTITY column.
/// FK target for Course and PartnerCourseGroup.
/// </summary>
public class CourseGroup
{
    public short Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
}
