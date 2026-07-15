using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class PartnerRepository : IPartnerRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PartnerRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = @"
        SELECT p.pkid, p.Name, p.AppKey, p.NameOnPartnerMenu, p.NameOnCourseDetailPage,
               p.DisplayOrder, p.ImageFilename
        FROM Partner p";

    public async Task<IEnumerable<Partner>> GetAllAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<Partner>($"{SelectColumns} ORDER BY p.DisplayOrder ASC");
    }

    public async Task<IEnumerable<Partner>> QueryAsync(PartnerQuery query)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add(@"(p.Name LIKE @Keyword
                        OR p.AppKey LIKE @Keyword
                        OR p.NameOnPartnerMenu LIKE @Keyword
                        OR p.NameOnCourseDetailPage LIKE @Keyword)");
            parameters.Add("Keyword", $"%{query.Keyword.Trim()}%");
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY p.DisplayOrder ASC";

        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<Partner>(sql, parameters);
    }

    public async Task<Partner?> GetByIdAsync(short pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<Partner>(
            $"{SelectColumns} WHERE p.pkid = @Pkid", new { Pkid = pkid });
    }

    public async Task<short> CreateAsync(PartnerRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.ExecuteScalarAsync<short>(
            @"INSERT INTO Partner (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage, DisplayOrder, ImageFilename)
              VALUES (@Name, @AppKey, @NameOnPartnerMenu, @NameOnCourseDetailPage, @DisplayOrder, @ImageFilename);
              SELECT CAST(SCOPE_IDENTITY() AS smallint);",
            new
            {
                request.Name,
                request.AppKey,
                request.NameOnPartnerMenu,
                request.NameOnCourseDetailPage,
                request.DisplayOrder,
                request.ImageFilename
            });
    }

    public async Task<bool> UpdateAsync(PartnerRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        var affected = await db.ExecuteAsync(
            @"UPDATE Partner
                 SET Name = @Name,
                     AppKey = @AppKey,
                     NameOnPartnerMenu = @NameOnPartnerMenu,
                     NameOnCourseDetailPage = @NameOnCourseDetailPage,
                     DisplayOrder = @DisplayOrder,
                     ImageFilename = @ImageFilename
               WHERE pkid = @Pkid",
            new
            {
                request.Pkid,
                request.Name,
                request.AppKey,
                request.NameOnPartnerMenu,
                request.NameOnCourseDetailPage,
                request.DisplayOrder,
                request.ImageFilename
            });
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(short pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        var affected = await db.ExecuteAsync(
            "DELETE FROM Partner WHERE pkid = @Pkid", new { Pkid = pkid });
        return affected > 0;
    }
}
