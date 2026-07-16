using System.Data;
using CMS.API.Auditing;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public class FeaturedPromoItemRepository : IFeaturedPromoItemRepository
{
    private const string TableName = "FeaturedPromoItem";

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRowAuditWriter _audit;

    public FeaturedPromoItemRepository(IDbConnectionFactory connectionFactory, IRowAuditWriter audit)
    {
        _connectionFactory = connectionFactory;
        _audit = audit;
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

    // Base columns only (no joined labels) — the shape compared for audit so the changed-column
    // list reflects only the real FeaturedPromoItem columns, never a derived label.
    private const string AuditSelectColumns = @"
        SELECT f.pkid, f.ScheduleOn, f.TrainingCenter_pkid AS TrainingCenterPkid, f.Slot,
               f.Promotion_pkid AS PromotionPkid, f.Topic, f.Description
        FROM FeaturedPromoItem f";

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
        db.Open();
        using var tx = db.BeginTransaction();

        var pkid = await db.ExecuteScalarAsync<int>(
            @"INSERT INTO FeaturedPromoItem (ScheduleOn, TrainingCenter_pkid, Slot, Promotion_pkid, Topic, Description)
              VALUES (@ScheduleOn, @TrainingCenterPkid, @Slot, @PromotionPkid, @Topic, @Description);
              SELECT CAST(SCOPE_IDENTITY() AS int);",
            Params(request), tx);

        var created = await ReadForAuditAsync(db, tx, pkid);
        await _audit.LogInsertAsync(db, tx, TableName, created);

        tx.Commit();
        return pkid;
    }

    public async Task<bool> UpdateAsync(FeaturedPromoItemRequest request)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        var before = await ReadForAuditAsync(db, tx, request.Pkid);
        if (before is null)
        {
            tx.Rollback();
            return false;
        }

        await db.ExecuteAsync(
            @"UPDATE FeaturedPromoItem
                 SET ScheduleOn = @ScheduleOn, TrainingCenter_pkid = @TrainingCenterPkid, Slot = @Slot,
                     Promotion_pkid = @PromotionPkid, Topic = @Topic, Description = @Description
               WHERE pkid = @Pkid",
            Params(request), tx);

        var after = await ReadForAuditAsync(db, tx, request.Pkid);
        await _audit.LogUpdateAsync(db, tx, TableName, before, after);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(int pkid)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        var before = await ReadForAuditAsync(db, tx, pkid);
        if (before is null)
        {
            tx.Rollback();
            return false;
        }

        await db.ExecuteAsync("DELETE FROM FeaturedPromoItem WHERE pkid = @Pkid", new { Pkid = pkid }, tx);
        await _audit.LogDeleteAsync(db, tx, TableName, before);

        tx.Commit();
        return true;
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

    /// <summary>Read the current row (base columns only) on the caller's connection/transaction for auditing.</summary>
    private static Task<FeaturedPromoItem?> ReadForAuditAsync(IDbConnection db, IDbTransaction tx, int pkid) =>
        db.QuerySingleOrDefaultAsync<FeaturedPromoItem>(
            $"{AuditSelectColumns} WHERE f.pkid = @Pkid", new { Pkid = pkid }, tx);
}
