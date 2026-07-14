using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class LookupRepository : ILookupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LookupRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<AppUserLookup>> GetAppUsersAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<AppUserLookup>(
            "SELECT UserId, UserName FROM AppUser ORDER BY UserName ASC");
    }
}
