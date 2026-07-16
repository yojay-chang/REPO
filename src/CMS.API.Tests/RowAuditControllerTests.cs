using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

/// <summary>
/// Endpoint tests for <c>GET /api/rowaudit</c>: it filters the trail by <c>tableName</c> + <c>pkid</c>
/// and returns the matching entries newest first. The fake repository holds a Course #123 trail plus
/// noise for other tables/records; the real SQL filter + ordering is proven in
/// <see cref="RowAuditRepositoryTests"/>.
/// </summary>
public class RowAuditControllerTests : IClassFixture<AppRoleApiFactory>
{
    private readonly HttpClient _client;

    public RowAuditControllerTests(AppRoleApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_FiltersByTableAndPkid_ReturnsOnlyThatRecordsTrail()
    {
        var entries = await _client.GetFromJsonAsync<List<RowAuditEntry>>("/api/rowaudit?tableName=Course&pkid=123");

        Assert.NotNull(entries);
        Assert.Equal(3, entries!.Count);
        // The Partner #123 and Course #999 noise rows are excluded.
        Assert.All(entries, e => Assert.Contains(e.UserName, new[] { "alice", "bob", "carol" }));
    }

    [Fact]
    public async Task Get_ReturnsRowsNewestFirst()
    {
        var entries = await _client.GetFromJsonAsync<List<RowAuditEntry>>("/api/rowaudit?tableName=Course&pkid=123");

        Assert.NotNull(entries);
        var dates = entries!.Select(e => e.DateTime).ToList();
        Assert.Equal(dates.OrderByDescending(d => d).ToList(), dates);
        // Newest is bob's Update on 2026-06-04 14:30.
        Assert.Equal(new DateTime(2026, 6, 4, 14, 30, 0), entries[0].DateTime);
        Assert.Equal("bob", entries[0].UserName);
        Assert.Equal("Update", entries[0].ActionType);
        Assert.Equal("Title", entries[0].ActionDesc);
    }

    [Fact]
    public async Task Get_UnknownRecord_ReturnsEmptyList()
    {
        var entries = await _client.GetFromJsonAsync<List<RowAuditEntry>>("/api/rowaudit?tableName=Course&pkid=4242");

        Assert.NotNull(entries);
        Assert.Empty(entries!);
    }

    [Fact]
    public async Task Get_MissingTableName_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/rowaudit?pkid=123");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
