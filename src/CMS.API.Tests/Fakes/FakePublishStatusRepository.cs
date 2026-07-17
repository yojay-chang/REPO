using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>In-memory <see cref="IPublishStatusRepository"/> so controller tests run without a live SQL Server.</summary>
public class FakePublishStatusRepository : IPublishStatusRepository
{
    private readonly List<PublishStatus> _statuses = [];

    public FakePublishStatusRepository()
    {
        _statuses.Add(new PublishStatus { Pkid = 1, Description = "草稿", IsDraft = true, IsPublished = false, IsDiscontinued = false });
        _statuses.Add(new PublishStatus { Pkid = 2, Description = "已發布", IsDraft = false, IsPublished = true, IsDiscontinued = false });
        _statuses.Add(new PublishStatus { Pkid = 3, Description = "已停用", IsDraft = false, IsPublished = false, IsDiscontinued = true });
    }

    public Task<IEnumerable<PublishStatus>> GetAllAsync()
        => Task.FromResult(_statuses.OrderBy(s => s.Pkid).Select(Clone).AsEnumerable());

    public Task<IEnumerable<PublishStatus>> QueryAsync(PublishStatusQuery query)
    {
        IEnumerable<PublishStatus> result = _statuses;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(s => s.Description.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.IsDraft.HasValue)
            result = result.Where(s => s.IsDraft == query.IsDraft.Value);

        if (query.IsPublished.HasValue)
            result = result.Where(s => s.IsPublished == query.IsPublished.Value);

        if (query.IsDiscontinued.HasValue)
            result = result.Where(s => s.IsDiscontinued == query.IsDiscontinued.Value);

        return Task.FromResult(result.OrderBy(s => s.Pkid).Select(Clone).AsEnumerable());
    }

    public Task<PublishStatus?> GetByIdAsync(byte pkid)
    {
        var status = _statuses.FirstOrDefault(s => s.Pkid == pkid);
        return Task.FromResult(status is null ? null : Clone(status));
    }

    public Task<bool> ExistsAsync(byte pkid)
        => Task.FromResult(_statuses.Any(s => s.Pkid == pkid));

    public Task<byte> CreateAsync(PublishStatusRequest request)
    {
        _statuses.Add(new PublishStatus
        {
            Pkid = request.Pkid,
            Description = request.Description,
            IsDraft = request.IsDraft,
            IsPublished = request.IsPublished,
            IsDiscontinued = request.IsDiscontinued
        });
        return Task.FromResult(request.Pkid);
    }

    public Task<bool> UpdateAsync(PublishStatusRequest request)
    {
        var status = _statuses.FirstOrDefault(s => s.Pkid == request.Pkid);
        if (status is null)
            return Task.FromResult(false);

        status.Description = request.Description;
        status.IsDraft = request.IsDraft;
        status.IsPublished = request.IsPublished;
        status.IsDiscontinued = request.IsDiscontinued;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(byte pkid)
    {
        var removed = _statuses.RemoveAll(s => s.Pkid == pkid);
        return Task.FromResult(removed > 0);
    }

    private static PublishStatus Clone(PublishStatus s) => new()
    {
        Pkid = s.Pkid,
        Description = s.Description,
        IsDraft = s.IsDraft,
        IsPublished = s.IsPublished,
        IsDiscontinued = s.IsDiscontinued
    };
}
