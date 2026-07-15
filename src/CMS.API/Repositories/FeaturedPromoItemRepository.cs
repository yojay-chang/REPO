using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class FeaturedPromoItemRepository : IFeaturedPromoItemRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FeaturedPromoItemRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // PromoCode (Promotion2) and TrainingCenter Name are JOINed in as flat read-only labels.
    // Both FKs are NOT NULL → INNER JOIN. Single-type mapping — no multi-map / splitOn required.
    private const string SelectColumns = @"
        SELECT f.pkid, f.ScheduleOn, f.TrainingCenter_pkid AS TrainingCenterPkid, f.Slot,
               f.Promotion_pkid AS PromotionPkid, f.Topic, f.Description,
               p.PromoCode AS PromoCode, tc.Name AS TrainingCenterName
        FROM FeaturedPromoItem f
             INNER JOIN Promotion2 p      ON p.pkid  = f.Promotion_pkid
             INNER JOIN TrainingCenter tc ON tc.pkid = f.TrainingCenter_pkid";

    public async Task<IEnumerable<FeaturedPromoItem>> GetAllAsync()
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<FeaturedPromoItem>(
            $"{SelectColumns} ORDER BY f.ScheduleOn ASC, f.Slot ASC");
    }

    public async Task<IEnumerable<FeaturedPromoItem>> QueryAsync(FeaturedPromoItemQuery query)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (query.TrainingCenterPkid.HasValue)
        {
            where.Add("f.TrainingCenter_pkid = @TrainingCenterPkid");
            parameters.Add("TrainingCenterPkid", query.TrainingCenterPkid.Value);
        }

        if (query.ScheduleOnFrom.HasValue)
        {
            where.Add("f.ScheduleOn >= @ScheduleOnFrom");
            parameters.Add("ScheduleOnFrom", query.ScheduleOnFrom.Value);
        }

        if (query.ScheduleOnTo.HasValue)
        {
            where.Add("f.ScheduleOn <= @ScheduleOnTo");
            parameters.Add("ScheduleOnTo", query.ScheduleOnTo.Value);
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY f.ScheduleOn ASC, f.Slot ASC";

        using var db = _connectionFactory.CreateConnection();
        return await db.QueryAsync<FeaturedPromoItem>(sql, parameters);
    }

    public async Task<FeaturedPromoItem?> GetByIdAsync(int pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<FeaturedPromoItem>(
            $"{SelectColumns} WHERE f.pkid = @Pkid", new { Pkid = pkid });
    }

    public async Task<int> CreateAsync(FeaturedPromoItemRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.ExecuteScalarAsync<int>(
            @"INSERT INTO FeaturedPromoItem (ScheduleOn, TrainingCenter_pkid, Slot, Promotion_pkid, Topic, Description)
              VALUES (@ScheduleOn, @TrainingCenterPkid, @Slot, @PromotionPkid, @Topic, @Description);
              SELECT CAST(SCOPE_IDENTITY() AS int);",
            Params(request));
    }

    public async Task<bool> UpdateAsync(FeaturedPromoItemRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        var affected = await db.ExecuteAsync(
            @"UPDATE FeaturedPromoItem
                 SET ScheduleOn = @ScheduleOn, TrainingCenter_pkid = @TrainingCenterPkid, Slot = @Slot,
                     Promotion_pkid = @PromotionPkid, Topic = @Topic, Description = @Description
               WHERE pkid = @Pkid",
            Params(request));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        var affected = await db.ExecuteAsync(
            "DELETE FROM FeaturedPromoItem WHERE pkid = @Pkid", new { Pkid = pkid });
        return affected > 0;
    }

    /// <summary>Scalar parameters shared by INSERT and UPDATE.</summary>
    private static object Params(FeaturedPromoItemRequest r) => new
    {
        r.Pkid,
        r.ScheduleOn,
        r.TrainingCenterPkid,
        r.Slot,
        r.PromotionPkid,
        r.Topic,
        r.Description,
    };
}
