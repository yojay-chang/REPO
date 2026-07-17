using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IFeaturedPromoItemRepository
{
    Task<IEnumerable<FeaturedPromoItem>> GetAllAsync();
    Task<IEnumerable<FeaturedPromoItem>> QueryAsync(FeaturedPromoItemQuery query);
    Task<FeaturedPromoItem?> GetByIdAsync(int pkid);
    Task<int> CreateAsync(FeaturedPromoItemRequest request);
    Task<bool> UpdateAsync(FeaturedPromoItemRequest request);
    Task<bool> DeleteAsync(int pkid);
}
