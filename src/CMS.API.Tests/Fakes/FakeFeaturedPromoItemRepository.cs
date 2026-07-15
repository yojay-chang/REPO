using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>In-memory <see cref="IFeaturedPromoItemRepository"/> so controller tests run without a live
/// SQL Server. Mimics the int IDENTITY PK by assigning the next pkid (max + 1) on create, and resolves
/// the JOINed PromoCode / TrainingCenter labels from small static maps that mirror
/// <see cref="FakeLookupRepository"/>.</summary>
public class FakeFeaturedPromoItemRepository : IFeaturedPromoItemRepository
{
    // Mirror FakeLookupRepository's promotions / training-centers.
    private static readonly Dictionary<int, string> Promotions = new()
    {
        [1] = "20251204_SkillTrainAI", [2] = "251211_GoogleAI", [3] = "20251215_n8n",
    };
    private static readonly Dictionary<short, string> TrainingCenters = new()
    {
        [1] = "台北", [2] = "新竹", [3] = "台中", [4] = "高雄", [5] = "線上研討會",
    };

    private readonly List<FeaturedPromoItem> _items = [];

    public FakeFeaturedPromoItemRepository()
    {
        // Week of 2026-03-16 (Mon) – 2026-03-22 (Sun), training center 台北 (1).
        Seed(1, new DateOnly(2026, 3, 16), 1, 1, 1, "成為能AI協作的程式設計師", "轉職就業養成班，三大主流語言任你選。");
        Seed(2, new DateOnly(2026, 3, 16), 1, 2, 2, "Google AI工具一次掌握", "不需技術基礎！最新 Google AI 實戰課程。");
        Seed(3, new DateOnly(2026, 3, 16), 1, 3, 3, "n8n自動化三部曲", "從自動化新手到企業級 AI 架構師學習路徑。");
        Seed(4, new DateOnly(2026, 3, 17), 1, 1, 3, "n8n自動化三部曲", "從自動化新手到企業級 AI 架構師學習路徑。");
        // Same week/day/slot but a different training center (新竹, 2) — excluded by the TrainingCenter filter.
        Seed(5, new DateOnly(2026, 3, 16), 2, 1, 1, "成為能AI協作的程式設計師", "新竹場次。");
        // Training center 台北 (1) but outside the 3/16–3/22 week — excluded by the one-week filter.
        Seed(6, new DateOnly(2026, 3, 30), 1, 1, 2, "Google AI工具一次掌握", "下一週的場次。");
    }

    private void Seed(int pkid, DateOnly scheduleOn, short tcPkid, byte slot, int promoPkid, string topic, string description)
        => _items.Add(new FeaturedPromoItem
        {
            Pkid = pkid, ScheduleOn = scheduleOn, TrainingCenterPkid = tcPkid, Slot = slot,
            PromotionPkid = promoPkid, Topic = topic, Description = description,
        });

    public Task<IEnumerable<FeaturedPromoItem>> GetAllAsync()
        => Task.FromResult(_items.OrderBy(i => i.ScheduleOn).ThenBy(i => i.Slot).Select(Clone).AsEnumerable());

    public Task<IEnumerable<FeaturedPromoItem>> QueryAsync(FeaturedPromoItemQuery query)
    {
        IEnumerable<FeaturedPromoItem> result = _items;

        if (query.TrainingCenterPkid.HasValue)
            result = result.Where(i => i.TrainingCenterPkid == query.TrainingCenterPkid.Value);
        if (query.ScheduleOnFrom.HasValue)
            result = result.Where(i => i.ScheduleOn >= query.ScheduleOnFrom.Value);
        if (query.ScheduleOnTo.HasValue)
            result = result.Where(i => i.ScheduleOn <= query.ScheduleOnTo.Value);

        return Task.FromResult(result.OrderBy(i => i.ScheduleOn).ThenBy(i => i.Slot).Select(Clone).AsEnumerable());
    }

    public Task<FeaturedPromoItem?> GetByIdAsync(int pkid)
    {
        var item = _items.FirstOrDefault(i => i.Pkid == pkid);
        return Task.FromResult(item is null ? null : Clone(item));
    }

    public Task<int> CreateAsync(FeaturedPromoItemRequest request)
    {
        var nextPkid = _items.Count == 0 ? 1 : _items.Max(i => i.Pkid) + 1;
        _items.Add(FromRequest(request, nextPkid));
        return Task.FromResult(nextPkid);
    }

    public Task<bool> UpdateAsync(FeaturedPromoItemRequest request)
    {
        var index = _items.FindIndex(i => i.Pkid == request.Pkid);
        if (index < 0)
            return Task.FromResult(false);

        _items[index] = FromRequest(request, request.Pkid);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int pkid)
    {
        var removed = _items.RemoveAll(i => i.Pkid == pkid);
        return Task.FromResult(removed > 0);
    }

    private static FeaturedPromoItem FromRequest(FeaturedPromoItemRequest r, int pkid) => new()
    {
        Pkid = pkid,
        ScheduleOn = r.ScheduleOn,
        TrainingCenterPkid = r.TrainingCenterPkid,
        Slot = r.Slot,
        PromotionPkid = r.PromotionPkid,
        Topic = r.Topic,
        Description = r.Description,
    };

    /// <summary>Copy with the JOINed PromoCode / TrainingCenter labels resolved (mirrors the repository SELECT).</summary>
    private static FeaturedPromoItem Clone(FeaturedPromoItem i) => new()
    {
        Pkid = i.Pkid,
        ScheduleOn = i.ScheduleOn,
        TrainingCenterPkid = i.TrainingCenterPkid,
        Slot = i.Slot,
        PromotionPkid = i.PromotionPkid,
        Topic = i.Topic,
        Description = i.Description,
        PromoCode = Promotions.GetValueOrDefault(i.PromotionPkid),
        TrainingCenterName = TrainingCenters.GetValueOrDefault(i.TrainingCenterPkid),
    };
}
