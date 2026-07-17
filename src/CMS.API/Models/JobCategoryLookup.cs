namespace CMS.API.Models;

/// <summary>Slim JobCategory lookup row for the Course job-categories multiselect.</summary>
public class JobCategoryLookup
{
    public short Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
}
