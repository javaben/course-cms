namespace CMS.API.Models;

/// <summary>
/// Search DTO for the FeaturedPromoItem (上稿作業) board.
/// The board filters by the active TrainingCenter tab and a Monday–Sunday week.
/// </summary>
public class FeaturedPromoItemQuery
{
    /// <summary>Active TrainingCenter tab (filters <c>TrainingCenter_pkid</c>). Null = all centers.</summary>
    public short? TrainingCenterPkid { get; set; }

    /// <summary>Monday of the selected week. When set, matches <c>ScheduleOn</c> in [WeekStart, WeekStart+6].</summary>
    public DateOnly? WeekStart { get; set; }
}
