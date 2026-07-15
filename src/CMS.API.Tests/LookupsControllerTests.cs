using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Tests;

/// <summary>Covers the lookups the FeaturedPromoItem board relies on: TrainingCenter tabs and PromoCode resolve.</summary>
public class LookupsControllerTests
{
    private static LookupsController Controller(InMemoryLookupRepository repo) => new(repo);

    private static InMemoryLookupRepository SeededRepo() =>
        new InMemoryLookupRepository()
            .SeedTrainingCenters(
                new TrainingCenterLookup { Pkid = 2, Name = "新竹" },
                new TrainingCenterLookup { Pkid = 1, Name = "台北" })
            .SeedPromotions(
                new PromotionLookup { Pkid = 10, PromoCode = "20251204_SkillTrainAI", Topic = "成為能AI協作的程式設計師", Description = "轉職就業養成班" })
            .SeedCertifications(
                new CertificationLookup { Pkid = 11, Label = "微軟 - Azure Fundamentals" },
                new CertificationLookup { Pkid = 10, Label = "甲骨文 - Oracle SQL" })
            .SeedJobCategories(
                new JobCategoryLookup { Pkid = 3, Description = "系統管理" },
                new JobCategoryLookup { Pkid = 1, Description = "軟體開發" });

    // ---- TrainingCenter tabs -------------------------------------------

    [Fact]
    public async Task GetTrainingCenters_returns_ordered_tabs()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetTrainingCenters(CancellationToken.None);

        var rows = Assert.IsAssignableFrom<IReadOnlyList<TrainingCenterLookup>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal([1, 2], rows.Select(r => r.Pkid).ToArray());
        Assert.Equal("台北", rows[0].Label);
    }

    // ---- PromoCode lookup ----------------------------------------------

    [Fact]
    public async Task GetPromotionByCode_resolves_a_known_code()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetPromotionByCode("20251204_SkillTrainAI", CancellationToken.None);

        var promo = Assert.IsType<PromotionLookup>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(10, promo.Pkid);
        Assert.Equal("成為能AI協作的程式設計師", promo.Topic);
    }

    [Fact]
    public async Task GetPromotionByCode_returns_404_for_unknown_code()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetPromotionByCode("does-not-exist", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Course n-n pickers --------------------------------------------

    [Fact]
    public async Task GetCertifications_returns_rows_ordered_by_label()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetCertifications(CancellationToken.None);

        var rows = Assert.IsAssignableFrom<IReadOnlyList<CertificationLookup>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(2, rows.Count);
        // Fake orders by label ordinal: 微 (U+5FAE) sorts before 甲 (U+7532).
        Assert.Equal("微軟 - Azure Fundamentals", rows[0].Label);
        Assert.Equal("甲骨文 - Oracle SQL", rows[1].Label);
    }

    [Fact]
    public async Task GetJobCategories_returns_rows_ordered_by_description()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetJobCategories(CancellationToken.None);

        var rows = Assert.IsAssignableFrom<IReadOnlyList<JobCategoryLookup>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(new short[] { 3, 1 }, rows.Select(r => r.Pkid).ToArray()); // 系統管理 before 軟體開發 (ordinal)
    }
}
