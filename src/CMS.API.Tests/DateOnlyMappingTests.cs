using System.Collections;
using System.Data.Common;
using Dapper;

namespace CMS.API.Tests;

/// <summary>
/// Regression guard for the Course <c>date</c> columns (ScheduleOn/ScheduleOff → <see cref="DateOnly"/>).
///
/// Microsoft.Data.SqlClient hands Dapper a <see cref="DateTime"/> for a SQL <c>date</c> column. Dapper
/// caches a compiled deserializer per type, and it only routes the column through a custom type handler
/// if that handler is registered <b>before</b> the first deserializer for the type is built. Program.cs
/// registers <c>DateOnlyTypeHandler</c> as its very first statement for exactly this reason.
///
/// Booting <see cref="AppRoleApiFactory"/> runs Program's startup (registering the handler). This test
/// then exercises Dapper's real deserializer against a DateOnly-shaped row; if the registration is
/// missing it fails with the same "Invalid cast from 'System.DateTime' to 'System.DateOnly'" the app hit.
/// </summary>
public class DateOnlyMappingTests : IClassFixture<AppRoleApiFactory>
{
    public DateOnlyMappingTests(AppRoleApiFactory factory)
    {
        // Force Program startup (and thus SqlMapper.AddTypeHandler) to run before deserializing.
        _ = factory.CreateClient();
    }

    private sealed class ScheduleRow
    {
        public int Pkid { get; set; }
        public DateOnly ScheduleOn { get; set; }
        public DateOnly? ScheduleOff { get; set; }
    }

    [Fact]
    public void Dapper_MapsSqlDateColumn_ToDateOnly()
    {
        using var reader = new DateColumnReader();
        Assert.True(reader.Read());

        var deserialize = SqlMapper.GetTypeDeserializer(typeof(ScheduleRow), reader);
        var row = (ScheduleRow)deserialize(reader)!;

        Assert.Equal(new DateOnly(2024, 4, 15), row.ScheduleOn);
        Assert.Equal(new DateOnly(2025, 4, 15), row.ScheduleOff);
    }

    [Fact]
    public void Dapper_MapsNullSqlDateColumn_ToNullDateOnly()
    {
        using var reader = new DateColumnReader(nullOff: true);
        Assert.True(reader.Read());

        var deserialize = SqlMapper.GetTypeDeserializer(typeof(ScheduleRow), reader);
        var row = (ScheduleRow)deserialize(reader)!;

        Assert.Equal(new DateOnly(2024, 4, 15), row.ScheduleOn);
        Assert.Null(row.ScheduleOff);
    }

    /// <summary>One-row reader that returns <see cref="DateTime"/> for its date columns, like SqlClient does.</summary>
    private sealed class DateColumnReader : DbDataReader
    {
        private readonly string[] _names = ["Pkid", "ScheduleOn", "ScheduleOff"];
        private readonly object[] _vals;
        private int _row = -1;

        public DateColumnReader(bool nullOff = false)
        {
            _vals =
            [
                1,
                new DateTime(2024, 4, 15),
                nullOff ? DBNull.Value : new DateTime(2025, 4, 15),
            ];
        }

        public override bool Read() => ++_row == 0;
        public override int FieldCount => _names.Length;
        public override string GetName(int i) => _names[i];
        public override Type GetFieldType(int i) => (_vals[i] ?? DBNull.Value).GetType();
        public override object GetValue(int i) => _vals[i] ?? DBNull.Value;
        public override int GetOrdinal(string name) => Array.IndexOf(_names, name);
        public override bool IsDBNull(int i) => _vals[i] is null or DBNull;
        public override object this[int i] => GetValue(i);
        public override object this[string name] => GetValue(GetOrdinal(name));

        public override int Depth => 0;
        public override bool HasRows => true;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override int GetValues(object[] values) { for (var i = 0; i < _vals.Length; i++) values[i] = GetValue(i); return _vals.Length; }
        public override bool GetBoolean(int i) => (bool)GetValue(i);
        public override byte GetByte(int i) => (byte)GetValue(i);
        public override long GetBytes(int i, long o, byte[]? b, int bo, int l) => 0;
        public override char GetChar(int i) => (char)GetValue(i);
        public override long GetChars(int i, long o, char[]? b, int bo, int l) => 0;
        public override DateTime GetDateTime(int i) => (DateTime)GetValue(i);
        public override decimal GetDecimal(int i) => (decimal)GetValue(i);
        public override double GetDouble(int i) => (double)GetValue(i);
        public override float GetFloat(int i) => (float)GetValue(i);
        public override Guid GetGuid(int i) => (Guid)GetValue(i);
        public override short GetInt16(int i) => (short)GetValue(i);
        public override int GetInt32(int i) => (int)GetValue(i);
        public override long GetInt64(int i) => (long)GetValue(i);
        public override string GetString(int i) => (string)GetValue(i);
        public override string GetDataTypeName(int i) => GetFieldType(i).Name;
        public override IEnumerator GetEnumerator() => _vals.GetEnumerator();
        public override bool NextResult() => false;
    }
}
