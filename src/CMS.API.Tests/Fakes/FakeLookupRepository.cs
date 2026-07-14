using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

public class FakeLookupRepository : ILookupRepository
{
    public Task<IEnumerable<AppUserLookup>> GetAppUsersAsync()
        => Task.FromResult(new[]
        {
            new AppUserLookup { UserId = "helen", UserName = "helen" },
            new AppUserLookup { UserId = "Jenny_Tsao", UserName = "Jenny_Tsao" },
            new AppUserLookup { UserId = "miles", UserName = "Miles Sun" }
        }.AsEnumerable());
}
