namespace CMS.API.Models;

/// <summary>Slim lookup row for the Course ↔ Certification n-n picker.
/// <see cref="Label"/> is built in SQL as "Partner.Name - Certification.Title" (Title RTRIM'd).</summary>
public class CertificationLookup
{
    public int Pkid { get; set; }             // 主代碼
    public string Label { get; set; } = "";   // e.g. "微軟 - MCSA 認證"
}
