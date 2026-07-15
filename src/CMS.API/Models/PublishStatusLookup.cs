namespace CMS.API.Models;

/// <summary>Slim PublishStatus lookup row for FK dropdowns in referencing features (Course, Promotion2).</summary>
public class PublishStatusLookup
{
    public byte Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
}
