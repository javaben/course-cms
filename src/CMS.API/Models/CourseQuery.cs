namespace CMS.API.Models;

/// <summary>Search DTO for Course. Each non-null field adds one WHERE clause.</summary>
public class CourseQuery
{
    public string? Keyword { get; set; }              // LIKE across Title/OfficialTitle/CourseId/ProdCourseId/FriendlyUrl
    public short? PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte? PublishStatusPkid { get; set; }
    public DateOnly? ScheduleOnFrom { get; set; }
    public DateOnly? ScheduleOnTo { get; set; }
    public DateOnly? ScheduleOffFrom { get; set; }
    public DateOnly? ScheduleOffTo { get; set; }
    public bool? CanRepeat { get; set; }
}
