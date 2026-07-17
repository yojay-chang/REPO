namespace CMS.API.Models;

/// <summary>Slim Certification lookup row for the Course certifications multiselect.</summary>
public class CertificationLookup
{
    public int Pkid { get; set; }
    /// <summary>Certification.Title (nchar(100), RTRIM-ed; may be null).</summary>
    public string? Title { get; set; }
}
