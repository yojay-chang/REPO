namespace CMS.API.Models;

/// <summary>Slim TrainingCenter lookup row for the FeaturedPromoItem scheduler tabs (pkid + Name).</summary>
public class TrainingCenterLookup
{
    public short Pkid { get; set; }
    public string Name { get; set; } = string.Empty;
}
