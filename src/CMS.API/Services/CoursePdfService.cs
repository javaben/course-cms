using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using CMS.API.Models;
using QRCoder;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CMS.API.Services;

/// <summary>
/// QuestPDF implementation of the course flyer. Layout is deliberately "correct and readable,"
/// not pixel-tuned (the fluent API is the least-transferable skill — see the feature plan).
///
/// The static constructor sets the QuestPDF Community license and registers the embedded
/// Noto Sans TC faces exactly once, so the service is ready whether it's resolved from DI in the
/// web host or newed up directly in a unit test — no startup-ordering dependency.
/// </summary>
public sealed class CoursePdfService : ICoursePdfService
{
    /// <summary>Family name inside both embedded faces (verified in the name table).</summary>
    public const string FontFamily = "Noto Sans TC";

    // Muted greys for the type hierarchy (kicker / subtitle / labels / url).
    private const string InkMuted = "#5B5B5B";
    private const string InkFaint = "#8A8A8A";
    private const string Rule = "#E0E0E0";

    private readonly string _publicSiteBaseUrl;

    static CoursePdfService()
    {
        // Community license: valid for this personal/learning build (see feature plan, T1 fork).
        QuestPDF.Settings.License = LicenseType.Community;
        RegisterEmbeddedFont("NotoSansTC-Regular.ttf");
        RegisterEmbeddedFont("NotoSansTC-Bold.ttf");
    }

    /// <param name="publicSiteBaseUrl">
    /// Base of the public course site, e.g. <c>https://www.uuu.com.tw</c> (from
    /// <c>PublicSite:BaseUrl</c>). The QR encodes <c>{base}/Course/Show/{pkid}/{courseId}</c> —
    /// the exact string the on-page QR uses, so the printed and on-screen codes match.
    /// </param>
    public CoursePdfService(string publicSiteBaseUrl)
    {
        _publicSiteBaseUrl = publicSiteBaseUrl.TrimEnd('/');
    }

    /// <summary>Force the static constructor to run at startup so a bad font/license fails at boot.</summary>
    public static void EnsureConfigured() { }

    public byte[] Build(Course course, IReadOnlyList<string> certLabels)
    {
        ArgumentNullException.ThrowIfNull(course);
        certLabels ??= [];

        var qrUrl = BuildCourseUrl(course);
        var qrPng = BuildQrPng(qrUrl);

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(11).LineHeight(1.5f));

                page.Content().Column(col =>
                {
                    col.Spacing(14);
                    ComposeHeader(col, course);
                    ComposeMetaRow(col, course);
                    ComposeBody(col, course, certLabels);
                });

                page.Footer().PaddingTop(12).Element(c => ComposeQrFooter(c, qrPng, qrUrl));
            });
        }).GeneratePdf();
    }

    // ---- Header: kicker (partner) → title → official title -------------------
    private static void ComposeHeader(ColumnDescriptor col, Course course)
    {
        col.Item().Column(head =>
        {
            head.Spacing(3);

            var partner = course.Partner?.Name;
            if (!string.IsNullOrWhiteSpace(partner))
                head.Item().Text(partner.Trim().ToUpperInvariant())
                    .FontSize(10).Bold().FontColor(InkMuted).LetterSpacing(0.08f);

            head.Item().Text(course.Title).FontSize(23).Bold();

            if (!string.IsNullOrWhiteSpace(course.OfficialTitle))
                head.Item().Text(course.OfficialTitle!.Trim()).FontSize(13).FontColor(InkMuted);
        });

        col.Item().LineHorizontal(1).LineColor(Rule);
    }

    // ---- Meta row: labeled, unit-bearing stat pairs --------------------------
    private static void ComposeMetaRow(ColumnDescriptor col, Course course)
    {
        var stats = new List<(string label, string value)>
        {
            ("學費 Fee", FormatPrice(course.ListPrice)),
        };
        if (course.Hour > 0) stats.Add(("時數 Hours", $"{course.Hour} 小時"));
        if (course.LearningCredit > 0) stats.Add(("學分 Credits", course.LearningCredit.ToString("0.##")));

        col.Item().Row(row =>
        {
            foreach (var (label, value) in stats)
            {
                row.AutoItem().PaddingRight(28).Column(cell =>
                {
                    cell.Item().Text(label).FontSize(9).FontColor(InkFaint);
                    cell.Item().Text(value).FontSize(14).Bold();
                });
            }
        });
    }

    // ---- Body: fixed section order, empty sections hidden --------------------
    private static void ComposeBody(ColumnDescriptor col, Course course, IReadOnlyList<string> certLabels)
    {
        Section(col, "課程目標 Objective", course.Objective);
        Section(col, "適合對象 Target Audience", course.Target);
        Section(col, "課程大綱 Outline", course.Outline);

        if (certLabels.Count > 0)
        {
            col.Item().Column(sec =>
            {
                sec.Spacing(4);
                sec.Item().Text("對應認證 Certifications").FontSize(13).Bold();
                foreach (var label in certLabels)
                    sec.Item().Text($"• {label}");
            });
        }
    }

    /// <summary>A titled prose section — rendered only when its source field has content.</summary>
    private static void Section(ColumnDescriptor col, string heading, string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return;

        var text = ToPlainText(body);
        // Markup-only content (e.g. a stray "<br>") flattens to nothing — hide the section rather
        // than print a bare heading.
        if (text.Length == 0) return;

        col.Item().Column(sec =>
        {
            sec.Spacing(4);
            sec.Item().Text(heading).FontSize(13).Bold();
            sec.Item().Text(text);
        });
    }

    // ---- Fail-safe QR footer: image + caption + printed URL ------------------
    private static void ComposeQrFooter(IContainer container, byte[] qrPng, string qrUrl)
    {
        container.BorderTop(1).BorderColor(Rule).PaddingTop(10).Row(row =>
        {
            row.ConstantItem(84).Image(qrPng).FitArea();

            row.RelativeItem().PaddingLeft(12).AlignMiddle().Column(cap =>
            {
                cap.Spacing(3);
                cap.Item().Text("掃描查看課程 Scan to view course").FontSize(11).Bold();
                // The URL is printed as readable text so the sheet still works if the scan fails.
                cap.Item().Text(qrUrl).FontSize(9).FontColor(InkFaint);
            });
        });
    }

    // ---- helpers -------------------------------------------------------------

    // Elements whose *content* is markup plumbing, not prose — dropped whole, not just untagged.
    private static readonly Regex NonProseElements = new(
        @"<(script|style|head)\b[^>]*>.*?</\1\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // Comments and declarations, e.g. <!-- ... --> and <!DOCTYPE html>.
    private static readonly Regex CommentsAndDeclarations = new(
        @"<!--.*?-->|<![^>]*>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    // Block-level boundaries — become newlines so the author's line structure survives.
    private static readonly Regex BlockBoundaries = new(
        @"</?(br|p|div|li|tr|ul|ol|table|h[1-6]|blockquote|section)\b[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Any remaining tag. The letter right after "<" is required, so prose like "a < b" or "<5"
    // is left alone — only real tags match.
    private static readonly Regex AnyTag = new(
        @"</?[a-zA-Z][^>]*>",
        RegexOptions.Compiled);

    private static readonly Regex BlankLineRuns = new(@"\n{3,}", RegexOptions.Compiled);
    private static readonly Regex TrailingSpaces = new(@"[ \t]+(?=\n)|(?<=\n)[ \t]+", RegexOptions.Compiled);

    /// <summary>
    /// Flattens stored rich text to plain text for the sheet.
    ///
    /// The curated free-text columns are legacy <c>nvarchar(max)</c> that hold HTML — the web detail
    /// page renders it as markup, but QuestPDF draws a string verbatim, so unflattened content
    /// printed raw tags ("&lt;br&gt;", "&lt;span style=...&gt;") on the flyer; a few rows hold an
    /// entire pasted HTML document, which printed its CSS too.
    /// </summary>
    internal static string ToPlainText(string html)
    {
        var text = NonProseElements.Replace(html, "\n");
        text = CommentsAndDeclarations.Replace(text, string.Empty);
        text = BlockBoundaries.Replace(text, "\n");
        text = AnyTag.Replace(text, string.Empty);

        // Decode only after tags are gone, so an encoded "&lt;b&gt;" can't re-enter as a live tag.
        text = WebUtility.HtmlDecode(text);

        // &nbsp; decodes to U+00A0; normalise it to a plain space so the text wraps normally.
        text = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\u00A0', ' ');
        text = TrailingSpaces.Replace(text, string.Empty);
        text = BlankLineRuns.Replace(text, "\n\n");
        return text.Trim();
    }

    /// <summary>NT$-prefixed thousands, no trailing-zero noise; 0/negative → free (never "NT$0").</summary>
    internal static string FormatPrice(decimal listPrice)
        => listPrice <= 0 ? "免費 Free" : $"NT${listPrice.ToString("#,##0")}";

    internal string BuildCourseUrl(Course course)
        => $"{_publicSiteBaseUrl}/Course/Show/{course.Pkid}/{course.CourseId}";

    private static byte[] BuildQrPng(string url)
    {
        using var generator = new QRCodeGenerator();
        // ECC level M — the printed URL below is the real fallback, so no need for H's overhead.
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        // PngByteQRCode avoids System.Drawing.Common (Windows-only; breaks cross-platform).
        var png = new PngByteQRCode(data);
        return png.GetGraphic(20);
    }

    private static void RegisterEmbeddedFont(string fileName)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Embedded font '{fileName}' not found. Ensure it is an <EmbeddedResource> in CMS.API.csproj.");

        using var stream = asm.GetManifestResourceStream(resourceName)!;
        FontManager.RegisterFont(stream);
    }
}
