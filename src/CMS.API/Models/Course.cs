namespace CMS.API.Models;

/// <summary>
/// Response model for the Course table (課程) — the central content entity.
/// The primary key <see cref="Pkid"/> is an auto-generated <c>int</c> IDENTITY column.
/// FK labels (<see cref="Partner"/>, <see cref="CourseGroup"/>, <see cref="PublishStatus"/>) are
/// filled via multi-map LEFT JOINs; the two N-N id lists are populated on GET-by-id only.
/// </summary>
public class Course
{
    public int Pkid { get; set; }                     // 主代碼 (int IDENTITY)
    public string Title { get; set; } = "";           // 課程名稱
    public string? OfficialTitle { get; set; }        // 官方課程名稱
    public string CourseId { get; set; } = "";        // 簡介代碼
    public string ProdCourseId { get; set; } = "";    // 科目代碼
    public string FriendlyUrl { get; set; } = "";     // 友善網址
    public int DisplayOrder { get; set; }             // 顯示順序
    public short PartnerPkid { get; set; }            // 原廠 (Partner_pkid)
    public short? CourseGroupPkid { get; set; }       // 課程群組 (CourseGroup_pkid, nullable)
    public byte PublishStatusPkid { get; set; }       // 上架狀態 (PublishStatus_pkid)
    public DateOnly ScheduleOn { get; set; }          // 上架日期
    public DateOnly ScheduleOff { get; set; }         // 下架日期
    public short Hour { get; set; }                   // 時數
    public decimal ListPrice { get; set; }            // 定價
    public decimal LearningCredit { get; set; }       // 點數
    public string? Material { get; set; }             // 教材
    public string? Objective { get; set; }            // 課程目標
    public string? Target { get; set; }               // 適合對象
    public string? Prerequisites { get; set; }        // 先備知識
    public string? Outline { get; set; }              // 課程大綱 (nvarchar(max))
    public string? TowardCertOrExam { get; set; }     // 考試／認證說明 (nvarchar(max))
    public string? Note { get; set; }                 // 備註
    public string? OtherInfo { get; set; }            // 其他資訊
    public bool CanRepeat { get; set; }               // 允許重聽

    // FK nav objects (multi-map LEFT JOIN) — labels for list/detail:
    public PartnerLookup? Partner { get; set; }
    public CourseGroupLookup? CourseGroup { get; set; }
    public PublishStatusLookup? PublishStatus { get; set; }

    // N-N — populated on GET-by-id only:
    public List<int> CertificationPkids { get; set; } = [];   // 對應認證
    public List<short> JobCategoryPkids { get; set; } = [];   // 對應職務類別
}
