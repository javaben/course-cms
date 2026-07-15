using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Tests;

public class FeaturedPromoItemsControllerTests
{
    // Week of the spec screenshots: Monday 2026-03-16 .. Sunday 2026-03-22.
    private static readonly DateOnly Mon = new(2026, 3, 16);
    private static readonly DateOnly Tue = new(2026, 3, 17);
    private static readonly DateOnly NextMon = new(2026, 3, 23);

    private const short Taipei = 1;   // 台北
    private const short Hsinchu = 2;  // 新竹

    private static InMemoryFeaturedPromoItemRepository SeededRepo() =>
        new InMemoryFeaturedPromoItemRepository()
            .WithPromotions(
                (10, "20251204_SkillTrainAI"),
                (11, "251211_GoogleAI"),
                (12, "20251215_n8n"))
            .Seed(
                new FeaturedPromoItem { Pkid = 1, ScheduleOn = Mon, TrainingCenterPkid = Taipei, Slot = 1, PromotionPkid = 10, Topic = "成為能AI協作的程式設計師", Description = "轉職就業養成班" },
                new FeaturedPromoItem { Pkid = 2, ScheduleOn = Mon, TrainingCenterPkid = Taipei, Slot = 2, PromotionPkid = 11, Topic = "Google AI工具一次掌握", Description = "不需技術基礎" },
                new FeaturedPromoItem { Pkid = 3, ScheduleOn = Tue, TrainingCenterPkid = Taipei, Slot = 1, PromotionPkid = 12, Topic = "n8n自動化三部曲", Description = "從自動化新手到企業級" },
                new FeaturedPromoItem { Pkid = 4, ScheduleOn = Mon, TrainingCenterPkid = Hsinchu, Slot = 1, PromotionPkid = 11, Topic = "Google AI工具一次掌握", Description = "新竹場" },
                new FeaturedPromoItem { Pkid = 5, ScheduleOn = NextMon, TrainingCenterPkid = Taipei, Slot = 1, PromotionPkid = 10, Topic = "下週", Description = "下一週的資料" });

    private static FeaturedPromoItemsController Controller(InMemoryFeaturedPromoItemRepository repo) => new(repo);

    private static IReadOnlyList<FeaturedPromoItem> Rows(ActionResult<IReadOnlyList<FeaturedPromoItem>> result)
        => Assert.IsAssignableFrom<IReadOnlyList<FeaturedPromoItem>>(Assert.IsType<OkObjectResult>(result.Result).Value);

    private static async Task<byte> SlotOf(FeaturedPromoItemsController controller, int pkid)
    {
        var result = await controller.GetById(pkid, CancellationToken.None);
        return Assert.IsType<FeaturedPromoItem>(Assert.IsType<OkObjectResult>(result.Result).Value).Slot;
    }

    // ---- One-week ScheduleOn filter (Monday–Sunday) --------------------

    [Fact]
    public async Task Query_returns_only_the_selected_week_Monday_to_Sunday()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new FeaturedPromoItemQuery { TrainingCenterPkid = Taipei, WeekStart = Mon }, CancellationToken.None);

        var rows = Rows(result);
        Assert.Equal(3, rows.Count);                                 // 3/16 x2 + 3/17 x1
        Assert.All(rows, r => Assert.InRange(r.ScheduleOn, Mon, Mon.AddDays(6)));
        Assert.DoesNotContain(rows, r => r.ScheduleOn == NextMon);   // next week excluded
    }

    [Fact]
    public async Task Query_orders_by_ScheduleOn_then_Slot()
    {
        var controller = Controller(SeededRepo());

        var rows = Rows(await controller.Query(new FeaturedPromoItemQuery { TrainingCenterPkid = Taipei, WeekStart = Mon }, CancellationToken.None));

        Assert.Equal([1, 2, 3], rows.Select(r => r.Pkid).ToArray());  // Mon/slot1, Mon/slot2, Tue/slot1
    }

    // ---- TrainingCenter filter -----------------------------------------

    [Fact]
    public async Task Query_filters_by_TrainingCenter_tab()
    {
        var controller = Controller(SeededRepo());

        var rows = Rows(await controller.Query(new FeaturedPromoItemQuery { TrainingCenterPkid = Hsinchu, WeekStart = Mon }, CancellationToken.None));

        Assert.Single(rows);
        Assert.All(rows, r => Assert.Equal(Hsinchu, r.TrainingCenterPkid));
        Assert.Equal(4, rows[0].Pkid);
    }

    // ---- PromoCode join ------------------------------------------------

    [Fact]
    public async Task Query_projects_joined_PromoCode()
    {
        var controller = Controller(SeededRepo());

        var rows = Rows(await controller.Query(new FeaturedPromoItemQuery { TrainingCenterPkid = Taipei, WeekStart = Mon }, CancellationToken.None));

        Assert.Equal("20251204_SkillTrainAI", rows.Single(r => r.Pkid == 1).PromoCode);
        Assert.Equal("20251215_n8n", rows.Single(r => r.Pkid == 3).PromoCode);
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_returns_row()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById(2, CancellationToken.None);

        var row = Assert.IsType<FeaturedPromoItem>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("251211_GoogleAI", row.PromoCode);
        Assert.Equal((byte)2, row.Slot);
    }

    [Fact]
    public async Task GetById_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById(99, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Add -----------------------------------------------------------

    [Fact]
    public async Task Create_adds_row_and_returns_201_with_generated_pkid_and_PromoCode()
    {
        var controller = Controller(SeededRepo());
        var request = new FeaturedPromoItemRequest
        {
            ScheduleOn = Tue, TrainingCenterPkid = Taipei, Slot = 2, PromotionPkid = 11,
            Topic = "Google AI工具一次掌握", Description = "週二第二版位"
        };

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var row = Assert.IsType<FeaturedPromoItem>(created.Value);
        Assert.True(row.Pkid > 0);                          // IDENTITY assigned
        Assert.Equal("251211_GoogleAI", row.PromoCode);     // resolved via the join
        Assert.Equal(row.Pkid, created.RouteValues!["id"]);
    }

    [Fact]
    public async Task Create_returns_409_when_slot_already_taken()
    {
        var controller = Controller(SeededRepo());
        // 3/16 / Taipei / slot 1 is already occupied by pkid 1.
        var request = new FeaturedPromoItemRequest
        {
            ScheduleOn = Mon, TrainingCenterPkid = Taipei, Slot = 1, PromotionPkid = 12,
            Topic = "x", Description = "y"
        };

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_changes_fields()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new FeaturedPromoItemRequest
        {
            Pkid = 1, ScheduleOn = Mon, TrainingCenterPkid = Taipei, Slot = 1, PromotionPkid = 12,
            Topic = "改標題", Description = "改說明"
        };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var row = Assert.IsType<FeaturedPromoItem>(Assert.IsType<OkObjectResult>((await controller.GetById(1, CancellationToken.None)).Result).Value);
        Assert.Equal("改標題", row.Topic);
        Assert.Equal("20251215_n8n", row.PromoCode);        // re-resolved from the new Promotion_pkid
    }

    [Fact]
    public async Task Update_keeping_its_own_slot_does_not_409()
    {
        var controller = Controller(SeededRepo());
        // pkid 1 keeps its (3/16, Taipei, slot 1) — the excludePkid guard must not treat itself as a clash.
        var request = new FeaturedPromoItemRequest
        {
            Pkid = 1, ScheduleOn = Mon, TrainingCenterPkid = Taipei, Slot = 1, PromotionPkid = 10,
            Topic = "同版位編輯", Description = "不應衝突"
        };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_returns_409_when_moving_onto_another_rows_slot()
    {
        var controller = Controller(SeededRepo());
        // pkid 1 tries to take (3/16, Taipei, slot 2) which pkid 2 owns.
        var request = new FeaturedPromoItemRequest
        {
            Pkid = 1, ScheduleOn = Mon, TrainingCenterPkid = Taipei, Slot = 2, PromotionPkid = 10,
            Topic = "x", Description = "y"
        };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Update_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());
        var request = new FeaturedPromoItemRequest
        {
            Pkid = 99, ScheduleOn = Mon, TrainingCenterPkid = Taipei, Slot = 3, PromotionPkid = 10,
            Topic = "x", Description = "y"
        };

        var result = await controller.Update(request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_removes_row()
    {
        var controller = Controller(SeededRepo());

        Assert.IsType<NoContentResult>(await controller.Delete(3, CancellationToken.None));
        Assert.IsType<NotFoundResult>((await controller.GetById(3, CancellationToken.None)).Result);
    }

    [Fact]
    public async Task Delete_returns_404_when_missing()
    {
        var controller = Controller(SeededRepo());

        Assert.IsType<NotFoundResult>(await controller.Delete(99, CancellationToken.None));
    }

    // ---- Move slot (+ / --) --------------------------------------------

    [Fact]
    public async Task Move_down_swaps_with_the_occupant_of_the_target_slot()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);

        // pkid 1 (slot 1) moves down; pkid 2 (slot 2) on the same day/center swaps up to slot 1.
        var result = await controller.Move(1, new MoveSlotRequest { Direction = 1 }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal((byte)2, await SlotOf(controller, 1));
        Assert.Equal((byte)1, await SlotOf(controller, 2));
    }

    [Fact]
    public async Task Move_up_into_an_empty_slot_just_shifts()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);

        // pkid 3 is Tue/Taipei/slot1 and alone that day → moving down to slot 2 leaves slot 1 empty.
        var result = await controller.Move(3, new MoveSlotRequest { Direction = 1 }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal((byte)2, await SlotOf(controller, 3));
    }

    [Fact]
    public async Task Move_up_from_slot_1_is_rejected()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Move(1, new MoveSlotRequest { Direction = -1 }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
