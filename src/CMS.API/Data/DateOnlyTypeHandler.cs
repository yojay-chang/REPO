using System.Data;
using Dapper;

namespace CMS.API.Data;

/// <summary>
/// Maps SQL Server <c>date</c> ⇄ <see cref="DateOnly"/>. Microsoft.Data.SqlClient hands Dapper a
/// <see cref="DateTime"/> for a <c>date</c> column, which Dapper cannot cast to <see cref="DateOnly"/>
/// on its own — this handler bridges both directions (read and parameter). Registered in Program.cs.
/// Dapper routes <c>DateOnly?</c> values through the same handler.
/// </summary>
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value)
        => value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            string s => DateOnly.Parse(s),
            _ => DateOnly.FromDateTime(Convert.ToDateTime(value)),
        };
}
