using System.Data;
using System.Reflection;
using System.Security.Claims;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;
using Microsoft.AspNetCore.Http;

namespace CMS.API.Auditing;

/// <summary>
/// Cross-cutting row-audit writer (see <see cref="IRowAuditWriter"/>). The instance methods resolve
/// the acting user from the current request's JWT, build a <see cref="RowAudit"/> via the pure static
/// helpers below, and INSERT it with Dapper. The reflection logic is kept in static, side-effect-free
/// methods so it can be unit-tested without a database or an HTTP context.
/// </summary>
public class RowAuditWriter : IRowAuditWriter
{
    /// <summary>Fallback UserName when the request has no authenticated user.</summary>
    public const string SystemUserName = "system";

    /// <summary>ActionDesc is capped to the RowAudit column width.</summary>
    public const int MaxActionDescLength = 1000;

    private const string InsertSql = @"
        INSERT INTO RowAudit (TableName, UserName, PrimaryKeyValues, ActionType, ActionDesc, [DateTime])
        VALUES (@TableName, @UserName, @PrimaryKeyValues, @ActionType, @ActionDesc, @DateTime)";

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RowAuditWriter(IDbConnectionFactory connectionFactory, IHttpContextAccessor httpContextAccessor)
    {
        _connectionFactory = connectionFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    // ---- Stand-alone forms (open their own connection) ----------------------------------------

    public async Task LogInsertAsync<T>(string tableName, T entity)
    {
        using var db = _connectionFactory.CreateConnection();
        await LogInsertAsync(db, null, tableName, entity);
    }

    public async Task LogUpdateAsync<T>(string tableName, T before, T after)
    {
        using var db = _connectionFactory.CreateConnection();
        await LogUpdateAsync(db, null, tableName, before, after);
    }

    public async Task LogDeleteAsync<T>(string tableName, T entity)
    {
        using var db = _connectionFactory.CreateConnection();
        await LogDeleteAsync(db, null, tableName, entity);
    }

    // ---- Same-connection / transaction forms --------------------------------------------------

    public Task LogInsertAsync<T>(IDbConnection connection, IDbTransaction? transaction, string tableName, T entity)
    {
        var row = BuildInsert(tableName, entity!, CurrentUserName(), DateTime.Now);
        return connection.ExecuteAsync(InsertSql, row, transaction);
    }

    public Task LogUpdateAsync<T>(IDbConnection connection, IDbTransaction? transaction, string tableName, T before, T after)
    {
        var row = BuildUpdate(tableName, before!, after!, CurrentUserName(), DateTime.Now);
        // Nothing changed → no audit row.
        return row is null ? Task.CompletedTask : connection.ExecuteAsync(InsertSql, row, transaction);
    }

    public Task LogDeleteAsync<T>(IDbConnection connection, IDbTransaction? transaction, string tableName, T entity)
    {
        var row = BuildDelete(tableName, entity!, CurrentUserName(), DateTime.Now);
        return connection.ExecuteAsync(InsertSql, row, transaction);
    }

    private string CurrentUserName() => ResolveUserName(_httpContextAccessor.HttpContext?.User);

    // ---- Pure logic (unit-tested) -------------------------------------------------------------

    /// <summary>Build the audit row for an insert — ActionDesc is the entity's first string property.</summary>
    public static RowAudit BuildInsert(string tableName, object entity, string userName, DateTime timestamp) =>
        new()
        {
            TableName = tableName,
            UserName = userName,
            PrimaryKeyValues = GetPrimaryKeyValue(entity),
            ActionType = "Insert",
            ActionDesc = TruncateDesc(GetFirstStringPropertyValue(entity)),
            DateTime = timestamp,
        };

    /// <summary>Build the audit row for a delete — ActionDesc is the entity's first string property.</summary>
    public static RowAudit BuildDelete(string tableName, object entity, string userName, DateTime timestamp) =>
        new()
        {
            TableName = tableName,
            UserName = userName,
            PrimaryKeyValues = GetPrimaryKeyValue(entity),
            ActionType = "Delete",
            ActionDesc = TruncateDesc(GetFirstStringPropertyValue(entity)),
            DateTime = timestamp,
        };

    /// <summary>
    /// Build the audit row for an update — ActionDesc is a comma-separated list of the changed property
    /// names. Returns <c>null</c> when nothing changed, so the caller writes no row.
    /// </summary>
    public static RowAudit? BuildUpdate(string tableName, object before, object after, string userName, DateTime timestamp)
    {
        var changed = GetChangedPropertyNames(before, after);
        if (changed.Count == 0)
            return null;

        return new RowAudit
        {
            TableName = tableName,
            UserName = userName,
            PrimaryKeyValues = GetPrimaryKeyValue(after),
            ActionType = "Update",
            ActionDesc = TruncateDesc(string.Join(", ", changed)),
            DateTime = timestamp,
        };
    }

    /// <summary>
    /// Resolve the acting user from the request principal's <c>userName</c> claim (the claim set by
    /// <see cref="Auth.JwtTokenGenerator"/>), falling back to the <see cref="ClaimTypes.Name"/> claim
    /// and finally to <see cref="SystemUserName"/> when there is no authenticated user.
    /// </summary>
    public static string ResolveUserName(ClaimsPrincipal? user)
    {
        var name = user?.FindFirst("userName")?.Value ?? user?.FindFirst(ClaimTypes.Name)?.Value;
        return string.IsNullOrWhiteSpace(name) ? SystemUserName : name;
    }

    /// <summary>The entity's <c>pkid</c> property value as a string (case-insensitive match); "" if absent/null.</summary>
    public static string GetPrimaryKeyValue(object entity)
    {
        var pk = entity.GetType().GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, "pkid", StringComparison.OrdinalIgnoreCase));
        return pk?.GetValue(entity)?.ToString() ?? string.Empty;
    }

    /// <summary>The value of the first string-typed property in declaration order; <c>null</c> if none/unset.</summary>
    public static string? GetFirstStringPropertyValue(object entity)
    {
        var prop = entity.GetType().GetProperties()
            .FirstOrDefault(p => p.PropertyType == typeof(string));
        return prop?.GetValue(entity) as string;
    }

    /// <summary>The names of the properties whose value differs between <paramref name="before"/> and <paramref name="after"/>.</summary>
    public static IReadOnlyList<string> GetChangedPropertyNames(object before, object after)
    {
        var changed = new List<string>();
        foreach (var prop in before.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            var b = prop.GetValue(before);
            var a = prop.GetValue(after);
            if (!Equals(b, a))
                changed.Add(prop.Name);
        }
        return changed;
    }

    private static string? TruncateDesc(string? value) =>
        value is null || value.Length <= MaxActionDescLength ? value : value[..MaxActionDescLength];
}
