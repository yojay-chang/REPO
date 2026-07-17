namespace CMS.API.Models;

/// <summary>Write DTO for CourseGroup (create/update). Pkid is IDENTITY — used for UPDATE, ignored on INSERT.</summary>
public class CourseGroupRequest
{
    public short Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
}
