using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IPartnerRepository
{
    Task<IEnumerable<Partner>> GetAllAsync();
    Task<IEnumerable<Partner>> QueryAsync(PartnerQuery query);
    Task<Partner?> GetByIdAsync(short pkid);
    Task<short> CreateAsync(PartnerRequest request);
    Task<bool> UpdateAsync(PartnerRequest request);
    Task<bool> DeleteAsync(short pkid);
}
