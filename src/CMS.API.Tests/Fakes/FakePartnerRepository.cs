using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>In-memory <see cref="IPartnerRepository"/> so controller tests run without a live SQL Server.
/// Mimics the smallint IDENTITY PK by assigning the next pkid (max + 1) on create.</summary>
public class FakePartnerRepository : IPartnerRepository
{
    private readonly List<Partner> _partners = [];

    public FakePartnerRepository()
    {
        _partners.Add(new Partner { Pkid = 1, Name = "微軟", AppKey = "MS", NameOnPartnerMenu = "微軟原廠課程", NameOnCourseDetailPage = "微軟", DisplayOrder = 1, ImageFilename = "ms.png" });
        _partners.Add(new Partner { Pkid = 2, Name = "思科", AppKey = "CISCO", NameOnPartnerMenu = "思科網路課程", NameOnCourseDetailPage = "思科", DisplayOrder = 2, ImageFilename = null });
        _partners.Add(new Partner { Pkid = 3, Name = "紅帽", AppKey = "RH", NameOnPartnerMenu = "紅帽 Linux 課程", NameOnCourseDetailPage = "紅帽", DisplayOrder = 3, ImageFilename = "redhat.png" });
    }

    public Task<IEnumerable<Partner>> GetAllAsync()
        => Task.FromResult(_partners.OrderBy(p => p.DisplayOrder).Select(Clone).AsEnumerable());

    public Task<IEnumerable<Partner>> QueryAsync(PartnerQuery query)
    {
        IEnumerable<Partner> result = _partners;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(p =>
                p.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || p.AppKey.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || p.NameOnPartnerMenu.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || p.NameOnCourseDetailPage.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(result.OrderBy(p => p.DisplayOrder).Select(Clone).AsEnumerable());
    }

    public Task<Partner?> GetByIdAsync(short pkid)
    {
        var partner = _partners.FirstOrDefault(p => p.Pkid == pkid);
        return Task.FromResult(partner is null ? null : Clone(partner));
    }

    public Task<short> CreateAsync(PartnerRequest request)
    {
        var nextPkid = (short)(_partners.Count == 0 ? 1 : _partners.Max(p => p.Pkid) + 1);
        _partners.Add(new Partner
        {
            Pkid = nextPkid,
            Name = request.Name,
            AppKey = request.AppKey,
            NameOnPartnerMenu = request.NameOnPartnerMenu,
            NameOnCourseDetailPage = request.NameOnCourseDetailPage,
            DisplayOrder = request.DisplayOrder,
            ImageFilename = request.ImageFilename
        });
        return Task.FromResult(nextPkid);
    }

    public Task<bool> UpdateAsync(PartnerRequest request)
    {
        var partner = _partners.FirstOrDefault(p => p.Pkid == request.Pkid);
        if (partner is null)
            return Task.FromResult(false);

        partner.Name = request.Name;
        partner.AppKey = request.AppKey;
        partner.NameOnPartnerMenu = request.NameOnPartnerMenu;
        partner.NameOnCourseDetailPage = request.NameOnCourseDetailPage;
        partner.DisplayOrder = request.DisplayOrder;
        partner.ImageFilename = request.ImageFilename;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(short pkid)
    {
        var removed = _partners.RemoveAll(p => p.Pkid == pkid);
        return Task.FromResult(removed > 0);
    }

    private static Partner Clone(Partner p) => new()
    {
        Pkid = p.Pkid,
        Name = p.Name,
        AppKey = p.AppKey,
        NameOnPartnerMenu = p.NameOnPartnerMenu,
        NameOnCourseDetailPage = p.NameOnCourseDetailPage,
        DisplayOrder = p.DisplayOrder,
        ImageFilename = p.ImageFilename
    };
}
