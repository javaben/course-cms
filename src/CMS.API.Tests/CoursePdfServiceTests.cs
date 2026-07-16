using System.Text;
using CMS.API.Models;
using CMS.API.Services;

namespace CMS.API.Tests;

/// <summary>
/// Unit tests for the flyer layout. These are the automated CJK "proxy" — they prove the PDF is
/// valid, the font is embedded, and the degenerate/overflow cases don't crash. The one thing they
/// CANNOT prove is that glyphs render as real characters rather than □□□ boxes — that is the
/// mandatory manual gate (T8): open a PDF on a machine with Noto Sans TC uninstalled.
/// </summary>
public class CoursePdfServiceTests
{
    private static readonly DateOnly On = new(2026, 3, 1);
    private static readonly DateOnly Off = new(2036, 3, 1);

    private static CoursePdfService Service() => new("https://www.uuu.com.tw");

    private static Course CjkCourse() => new()
    {
        Pkid = 1, Title = "Azure 基礎認證課程", OfficialTitle = "Microsoft Azure Fundamentals (AZ-900)",
        CourseId = "AZ-900", ProdCourseId = "P-AZ900", FriendlyUrl = "azure-900",
        PartnerPkid = 1, PublishStatusPkid = 2, ScheduleOn = On, ScheduleOff = Off,
        Hour = 40, ListPrice = 12000m, LearningCredit = 3.5m,
        Objective = "了解雲端運算的核心概念，掌握 Azure 的基礎服務與計價模式。",
        Target = "資訊從業人員、準備考取 AZ-900 認證者。",
        Outline = "第一章 雲端概念\n第二章 核心服務\n第三章 安全與合規\n第四章 計價與支援",
        Partner = new PartnerLookup { Pkid = 1, Name = "微軟" },
        CertificationPkids = [10],
    };

    private static readonly string[] CertLabels = ["微軟 - AZ-900"];

    // ---- Valid PDF + embedded CJK font (the automated proxy) ---------------

    [Fact]
    public void Build_produces_a_valid_pdf()
    {
        var bytes = Service().Build(CjkCourse(), CertLabels);

        Assert.NotNull(bytes);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.True(bytes.Length > 15_000, $"expected embedded glyph data, got {bytes.Length} bytes");
    }

    [Fact]
    public void Build_embeds_the_noto_sans_tc_font()
    {
        var bytes = Service().Build(CjkCourse(), CertLabels);

        // SkiaSharp's PDF backend writes the embedded subset's PostScript name (e.g. "ABCDEF+NotoSansTC")
        // into the font descriptor. Its presence in the raw bytes proves the face was embedded, not
        // merely referenced — the single most important guard against a silent boxes-only flyer.
        var haystack = Encoding.Latin1.GetString(bytes);
        Assert.Contains("NotoSansTC", haystack, StringComparison.Ordinal);
    }

    // ---- Degenerate content: nothing throws, output stays valid ------------

    [Fact]
    public void Build_hides_sections_for_null_optional_fields()
    {
        var course = CjkCourse();
        course.OfficialTitle = null;
        course.Objective = null;
        course.Target = null;
        course.Outline = null;

        var bytes = Service().Build(course, CertLabels);

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Build_handles_an_all_empty_course_with_no_certs()
    {
        // Title only — every optional section empty, no certifications (A4 degenerate case).
        var course = new Course
        {
            Pkid = 7, Title = "未命名課程", CourseId = "TBD",
            PartnerPkid = 1, PublishStatusPkid = 1, ScheduleOn = On, ScheduleOff = Off,
        };

        var bytes = Service().Build(course, []);

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Build_paginates_a_very_long_outline()
    {
        var shortCourse = CjkCourse();
        var longCourse = CjkCourse();
        longCourse.Outline = string.Concat(Enumerable.Repeat(
            "本章節詳細說明雲端運算的架構、部署模型與實務案例，並提供大量練習題與情境演練。\n", 200));

        var shortPages = PageCount(Service().Build(shortCourse, CertLabels));
        var longPages = PageCount(Service().Build(longCourse, CertLabels));

        // A 200x-longer outline must flow onto additional pages, not clip.
        Assert.Equal(1, shortPages);
        Assert.True(longPages > 1, $"long outline should span multiple pages, got {longPages}");
    }

    /// <summary>Page count via the per-page /MediaBox marker SkiaSharp writes (uncompressed).</summary>
    private static int PageCount(byte[] pdf)
    {
        var text = Encoding.Latin1.GetString(pdf);
        return text.Split("/MediaBox").Length - 1;
    }

    // ---- Price formatting (B/A6): no trailing zeros, free never "NT$0" -----

    [Theory]
    [InlineData(0, "免費 Free")]
    [InlineData(-5, "免費 Free")]
    [InlineData(12000, "NT$12,000")]
    [InlineData(3.5, "NT$4")]        // rounds to whole NT dollars, no ".00" noise
    [InlineData(1234567, "NT$1,234,567")]
    public void FormatPrice_is_explicit(decimal input, string expected)
        => Assert.Equal(expected, CoursePdfService.FormatPrice(input));

    // ---- QR URL matches the on-page QR shape --------------------------------

    [Fact]
    public void BuildCourseUrl_matches_the_on_page_qr_shape()
    {
        var url = new CoursePdfService("https://www.uuu.com.tw/").BuildCourseUrl(CjkCourse());
        // Trailing slash on the base is trimmed; format is {base}/Course/Show/{pkid}/{courseId}.
        Assert.Equal("https://www.uuu.com.tw/Course/Show/1/AZ-900", url);
    }

    // ---- Escape hatch to eyeball a real PDF (T8 manual gate) ---------------

    [Fact]
    public void Dump_sample_pdf_when_requested()
    {
        var path = Environment.GetEnvironmentVariable("COURSE_PDF_DUMP");
        if (string.IsNullOrWhiteSpace(path)) return; // no-op unless explicitly asked

        File.WriteAllBytes(path, Service().Build(CjkCourse(), CertLabels));
    }
}
