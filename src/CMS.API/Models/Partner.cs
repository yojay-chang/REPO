namespace CMS.API.Models;

/// <summary>
/// Partner 合作廠商 — response model.
/// PK is <see cref="Pkid"/> (smallint), a <b>system-assigned</b> IDENTITY column.
/// FK target for Course, Certification, and PartnerCourseGroup.
/// </summary>
public class Partner
{
    public short Pkid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string NameOnPartnerMenu { get; set; } = string.Empty;
    public string NameOnCourseDetailPage { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ImageFilename { get; set; }
}
