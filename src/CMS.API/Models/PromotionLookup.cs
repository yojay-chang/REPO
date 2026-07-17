namespace CMS.API.Models;

/// <summary>Slim Promotion2 lookup row for the FeaturedPromoItem PromoCode autocomplete (pkid + PromoCode).</summary>
public class PromotionLookup
{
    public int Pkid { get; set; }
    public string PromoCode { get; set; } = string.Empty;
}
