using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class CourseGroupRepository : ICourseGroupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CourseGroupRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = @"
        SELECT cg.pkid, cg.Description
        FROM CourseGroup cg";

    public async Task<IEnumerable<CourseGroup>> GetAllAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<CourseGroup>($"{SelectColumns} ORDER BY cg.pkid DESC");
    }

    public async Task<IEnumerable<CourseGroup>> QueryAsync(CourseGroupQuery query)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("cg.Description LIKE @Keyword");
            parameters.Add("Keyword", $"%{query.Keyword.Trim()}%");
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY cg.pkid DESC";

        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<CourseGroup>(sql, parameters);
    }

    public async Task<CourseGroup?> GetByIdAsync(short pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<CourseGroup>(
            $"{SelectColumns} WHERE cg.pkid = @Pkid", new { Pkid = pkid });
    }

    public async Task<short> CreateAsync(CourseGroupRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.ExecuteScalarAsync<short>(
            @"INSERT INTO CourseGroup (Description)
              VALUES (@Description);
              SELECT CAST(SCOPE_IDENTITY() AS smallint);",
            new
            {
                request.Description
            });
    }

    public async Task<bool> UpdateAsync(CourseGroupRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        var affected = await db.ExecuteAsync(
            @"UPDATE CourseGroup
                 SET Description = @Description
               WHERE pkid = @Pkid",
            new
            {
                request.Pkid,
                request.Description
            });
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(short pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        var affected = await db.ExecuteAsync(
            "DELETE FROM CourseGroup WHERE pkid = @Pkid", new { Pkid = pkid });
        return affected > 0;
    }
}
