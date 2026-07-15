using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

public class FeaturedPromoItemsControllerTests : IClassFixture<AppRoleApiFactory>
{
    private readonly HttpClient _client;

    public FeaturedPromoItemsControllerTests(AppRoleApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // Monday–Sunday of the seeded week (2026-03-16 – 2026-03-22).
    private static readonly DateOnly WeekMonday = new(2026, 3, 16);
    private static readonly DateOnly WeekSunday = new(2026, 3, 22);

    private static FeaturedPromoItemRequest NewRequest() => new()
    {
        ScheduleOn = new DateOnly(2026, 3, 18),
        TrainingCenterPkid = 1,
        Slot = 2,
        PromotionPkid = 2,
        Topic = "Google AI工具一次掌握",
        Description = "不需技術基礎！最新 Google AI 實戰課程。",
    };

    // ---- List ----

    [Fact]
    public async Task GetAll_ReturnsSeededItemsWithJoinedLabels()
    {
        var items = await _client.GetFromJsonAsync<List<FeaturedPromoItem>>("/api/featured-promo-items");

        Assert.NotNull(items);
        var first = Assert.Single(items!, i => i.Pkid == 1);
        Assert.Equal("20251204_SkillTrainAI", first.PromoCode);
        Assert.Equal("台北", first.TrainingCenterName);
        Assert.Equal("成為能AI協作的程式設計師", first.Topic);
    }

    [Fact]
    public async Task GetAll_IsOrderedByScheduleOnThenSlot()
    {
        var items = await _client.GetFromJsonAsync<List<FeaturedPromoItem>>("/api/featured-promo-items");

        Assert.NotNull(items);
        var keys = items!.Select(i => (i.ScheduleOn, i.Slot)).ToList();
        Assert.Equal(keys.OrderBy(k => k.ScheduleOn).ThenBy(k => k.Slot), keys);
    }

    // ---- List (query filters) ----

    [Fact]
    public async Task Query_ByOneWeekRange_ExcludesOtherWeeks()
    {
        var response = await _client.PostAsJsonAsync("/api/featured-promo-items/query",
            new FeaturedPromoItemQuery { ScheduleOnFrom = WeekMonday, ScheduleOnTo = WeekSunday });
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<FeaturedPromoItem>>();
        Assert.NotNull(items);
        Assert.All(items!, i => Assert.InRange(i.ScheduleOn, WeekMonday, WeekSunday));
        // pkid 6 is 2026-03-30 — the following week — and must be excluded.
        Assert.DoesNotContain(items!, i => i.Pkid == 6);
        Assert.Contains(items!, i => i.Pkid == 1);
    }

    [Fact]
    public async Task Query_ByTrainingCenter_FiltersByTrainingCenterPkid()
    {
        var response = await _client.PostAsJsonAsync("/api/featured-promo-items/query",
            new FeaturedPromoItemQuery { TrainingCenterPkid = 1 });
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<FeaturedPromoItem>>();
        Assert.NotNull(items);
        Assert.All(items!, i => Assert.Equal((short)1, i.TrainingCenterPkid));
        // pkid 5 belongs to training center 2 (新竹) and must be excluded.
        Assert.DoesNotContain(items!, i => i.Pkid == 5);
    }

    [Fact]
    public async Task Query_ByTrainingCenterAndWeek_CombinesBothFilters()
    {
        var response = await _client.PostAsJsonAsync("/api/featured-promo-items/query",
            new FeaturedPromoItemQuery
            {
                TrainingCenterPkid = 1,
                ScheduleOnFrom = WeekMonday,
                ScheduleOnTo = WeekSunday,
            });
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<FeaturedPromoItem>>();
        Assert.NotNull(items);
        // Every row must satisfy BOTH filters (台北 + inside the week).
        Assert.All(items!, i =>
        {
            Assert.Equal((short)1, i.TrainingCenterPkid);
            Assert.InRange(i.ScheduleOn, WeekMonday, WeekSunday);
        });
        // The seeded 台北 week rows (pkids 1–4) are present; 5 (新竹) and 6 (next week) are excluded.
        Assert.Contains(items!, i => i.Pkid == 1);
        Assert.Contains(items!, i => i.Pkid == 4);
        Assert.DoesNotContain(items!, i => i.Pkid == 5);
        Assert.DoesNotContain(items!, i => i.Pkid == 6);
    }

    // ---- View ----

    [Fact]
    public async Task GetById_Existing_ReturnsItem()
    {
        var item = await _client.GetFromJsonAsync<FeaturedPromoItem>("/api/featured-promo-items/3");

        Assert.NotNull(item);
        Assert.Equal("20251215_n8n", item!.PromoCode);
        Assert.Equal((byte)3, item.Slot);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/featured-promo-items/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Add ----

    [Fact]
    public async Task Create_NewItem_ReturnsCreatedWithJoinedLabels()
    {
        var response = await _client.PostAsJsonAsync("/api/featured-promo-items", NewRequest());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<FeaturedPromoItem>();
        Assert.NotNull(created);
        Assert.True(created!.Pkid > 0);
        Assert.Equal("251211_GoogleAI", created.PromoCode);
        Assert.Equal("台北", created.TrainingCenterName);
    }

    [Fact]
    public async Task Create_MissingTopic_ReturnsBadRequest()
    {
        var request = NewRequest();
        request.Topic = "";

        var response = await _client.PostAsJsonAsync("/api/featured-promo-items", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingPromotion_ReturnsBadRequest()
    {
        var request = NewRequest();
        request.PromotionPkid = 0;

        var response = await _client.PostAsJsonAsync("/api/featured-promo-items", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_SlotOutOfRange_ReturnsBadRequest()
    {
        var request = NewRequest();
        request.Slot = 4;

        var response = await _client.PostAsJsonAsync("/api/featured-promo-items", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Edit ----

    [Fact]
    public async Task Update_ExistingItem_ChangesPromotionAndSlot()
    {
        var seed = await _client.PostAsJsonAsync("/api/featured-promo-items", NewRequest());
        var created = await seed.Content.ReadFromJsonAsync<FeaturedPromoItem>();

        var update = NewRequest();
        update.Pkid = created!.Pkid;
        update.Slot = 3;
        update.PromotionPkid = 3;
        update.Topic = "n8n自動化三部曲";

        var response = await _client.PutAsJsonAsync("/api/featured-promo-items", update);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<FeaturedPromoItem>();
        Assert.Equal((byte)3, updated!.Slot);
        Assert.Equal("20251215_n8n", updated.PromoCode);
        Assert.Equal("n8n自動化三部曲", updated.Topic);
    }

    [Fact]
    public async Task Update_MissingItem_ReturnsNotFound()
    {
        var update = NewRequest();
        update.Pkid = 777;

        var response = await _client.PutAsJsonAsync("/api/featured-promo-items", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Delete ----

    [Fact]
    public async Task Delete_ExistingItem_ReturnsNoContent()
    {
        var seed = await _client.PostAsJsonAsync("/api/featured-promo-items", NewRequest());
        var created = await seed.Content.ReadFromJsonAsync<FeaturedPromoItem>();

        var response = await _client.DeleteAsync($"/api/featured-promo-items/{created!.Pkid}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetch = await _client.GetAsync($"/api/featured-promo-items/{created.Pkid}");
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Delete_MissingItem_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/featured-promo-items/888");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Lookups ----

    [Fact]
    public async Task GetTrainingCentersLookup_ReturnsTabRows()
    {
        var centers = await _client.GetFromJsonAsync<List<TrainingCenterLookup>>("/api/lookups/training-centers");

        Assert.NotNull(centers);
        Assert.Contains(centers!, c => c.Pkid == 1 && c.Name == "台北");
        Assert.Contains(centers!, c => c.Pkid == 5 && c.Name == "線上研討會");
    }

    [Fact]
    public async Task GetPromotionsLookup_ReturnsPromoCodeRows()
    {
        var promotions = await _client.GetFromJsonAsync<List<PromotionLookup>>("/api/lookups/promotions");

        Assert.NotNull(promotions);
        Assert.Contains(promotions!, p => p.Pkid == 2 && p.PromoCode == "251211_GoogleAI");
    }
}
