namespace CMS.API.Models;

/// <summary>Search DTO for CourseGroup list filtering.</summary>
public class CourseGroupQuery
{
    /// <summary>LIKE match on Description.</summary>
    public string? Keyword { get; set; }
}
