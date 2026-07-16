using System.Security.Claims;
using CMS.API.Auditing;

namespace CMS.API.Tests;

/// <summary>
/// Unit tests for the pure reflection logic in <see cref="RowAuditWriter"/> — the part repositories
/// depend on being correct: which value becomes <c>ActionDesc</c>, which property names an update
/// reports as changed, how the pkid and the acting user are resolved, and the 1000-char truncation.
/// No database or HTTP context is required; the static builders are exercised directly.
/// </summary>
public class RowAuditWriterTests
{
    // A representative entity: int pkid first, then several string / non-string columns.
    private sealed class SampleEntity
    {
        public int Pkid { get; set; }
        public string Title { get; set; } = string.Empty;   // first string property → ActionDesc
        public string Code { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    private static readonly DateTime FixedTime = new(2026, 7, 15, 9, 30, 0, DateTimeKind.Utc);

    // ---- ActionDesc: first string property (Insert / Delete) ----------------------------------

    [Fact]
    public void BuildInsert_UsesFirstStringPropertyAsActionDesc()
    {
        var entity = new SampleEntity { Pkid = 7, Title = "Intro to Dapper", Code = "DAP-101" };

        var row = RowAuditWriter.BuildInsert("Course", entity, "helen", FixedTime);

        Assert.Equal("Course", row.TableName);
        Assert.Equal("Insert", row.ActionType);
        Assert.Equal("helen", row.UserName);
        Assert.Equal("7", row.PrimaryKeyValues);
        Assert.Equal("Intro to Dapper", row.ActionDesc); // Title, not Code — first string in declaration order
        Assert.Equal(FixedTime, row.DateTime);
    }

    [Fact]
    public void BuildDelete_UsesFirstStringPropertyAsActionDesc()
    {
        var entity = new SampleEntity { Pkid = 42, Title = "Deleted Course", Code = "DEL-1" };

        var row = RowAuditWriter.BuildDelete("Course", entity, "miles", FixedTime);

        Assert.Equal("Delete", row.ActionType);
        Assert.Equal("42", row.PrimaryKeyValues);
        Assert.Equal("Deleted Course", row.ActionDesc);
    }

    [Fact]
    public void GetFirstStringPropertyValue_ReturnsNull_WhenValueUnset()
    {
        // A null first-string value flows through as a null ActionDesc.
        var row = RowAuditWriter.BuildInsert("Thing", new NullTitleEntity { Pkid = 1 }, "system", FixedTime);
        Assert.Null(row.ActionDesc);
    }

    private sealed class NullTitleEntity
    {
        public int Pkid { get; set; }
        public string? Title { get; set; }   // null → ActionDesc null
    }

    // ---- ActionDesc: changed property names (Update) ------------------------------------------

    [Fact]
    public void BuildUpdate_ListsExactlyTheChangedPropertyNames()
    {
        var before = new SampleEntity { Pkid = 5, Title = "Old", Code = "C1", DisplayOrder = 1, IsActive = false };
        var after = new SampleEntity { Pkid = 5, Title = "New", Code = "C1", DisplayOrder = 2, IsActive = false };

        var row = RowAuditWriter.BuildUpdate("Course", before, after, "helen", FixedTime);

        Assert.NotNull(row);
        Assert.Equal("Update", row!.ActionType);
        Assert.Equal("5", row.PrimaryKeyValues);
        // Exactly the changed columns, comma-separated, in declaration order.
        Assert.Equal("Title, DisplayOrder", row.ActionDesc);
    }

    [Fact]
    public void GetChangedPropertyNames_ReturnsEmpty_WhenNothingChanged()
    {
        var before = new SampleEntity { Pkid = 5, Title = "Same", Code = "C1", DisplayOrder = 1, IsActive = true };
        var after = new SampleEntity { Pkid = 5, Title = "Same", Code = "C1", DisplayOrder = 1, IsActive = true };

        Assert.Empty(RowAuditWriter.GetChangedPropertyNames(before, after));
    }

    [Fact]
    public void BuildUpdate_ReturnsNull_WhenNothingChanged()
    {
        var before = new SampleEntity { Pkid = 5, Title = "Same", DisplayOrder = 1 };
        var after = new SampleEntity { Pkid = 5, Title = "Same", DisplayOrder = 1 };

        Assert.Null(RowAuditWriter.BuildUpdate("Course", before, after, "helen", FixedTime));
    }

    // ---- PrimaryKeyValues: reads pkid by reflection -------------------------------------------

    [Fact]
    public void GetPrimaryKeyValue_ReadsPkid()
    {
        Assert.Equal("99", RowAuditWriter.GetPrimaryKeyValue(new SampleEntity { Pkid = 99 }));
    }

    [Fact]
    public void GetPrimaryKeyValue_ReturnsEmpty_WhenNoPkidProperty()
    {
        Assert.Equal(string.Empty, RowAuditWriter.GetPrimaryKeyValue(new { Name = "no pk here" }));
    }

    // ---- UserName resolution ------------------------------------------------------------------

    [Fact]
    public void ResolveUserName_ReadsUserNameClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userName", "helen") }, "jwt"));
        Assert.Equal("helen", RowAuditWriter.ResolveUserName(principal));
    }

    [Fact]
    public void ResolveUserName_FallsBackToSystem_WhenNoAuthenticatedUser()
    {
        // No principal at all, and an empty principal, both fall back to "system".
        Assert.Equal("system", RowAuditWriter.ResolveUserName(null));
        Assert.Equal("system", RowAuditWriter.ResolveUserName(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    // ---- ActionDesc truncation ----------------------------------------------------------------

    [Fact]
    public void ActionDesc_TruncatesAt1000Characters()
    {
        var longTitle = new string('x', 1500);
        var row = RowAuditWriter.BuildInsert("Course", new SampleEntity { Pkid = 1, Title = longTitle }, "system", FixedTime);

        Assert.Equal(RowAuditWriter.MaxActionDescLength, row.ActionDesc!.Length);
        Assert.Equal(new string('x', RowAuditWriter.MaxActionDescLength), row.ActionDesc);
    }

    [Fact]
    public void ActionDesc_NotTruncated_WhenAtOrUnderLimit()
    {
        var exactly = new string('y', RowAuditWriter.MaxActionDescLength);
        var row = RowAuditWriter.BuildInsert("Course", new SampleEntity { Pkid = 1, Title = exactly }, "system", FixedTime);

        Assert.Equal(RowAuditWriter.MaxActionDescLength, row.ActionDesc!.Length);
    }
}
