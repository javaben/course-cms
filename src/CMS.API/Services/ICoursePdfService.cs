using CMS.API.Models;

namespace CMS.API.Services;

/// <summary>
/// Renders a single <see cref="Course"/> as a shareable one/two-page PDF flyer (課程宣傳單).
/// Pure layout + bytes — no HTTP, no data access — so it is unit-testable without a web host.
/// </summary>
public interface ICoursePdfService
{
    /// <summary>
    /// Build the flyer for <paramref name="course"/>. <paramref name="certLabels"/> are the resolved
    /// certification names (e.g. "微軟 - AZ-900"); pass an empty list to omit the certification section.
    /// Returns the finished PDF as bytes (<c>application/pdf</c>).
    /// </summary>
    byte[] Build(Course course, IReadOnlyList<string> certLabels);
}
