using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="ICourseRepository"/> for controller tests — mirrors the SQL behaviour:
/// FK nav labels (from the seeded lookup maps), keyword + filter clauses, DisplayOrder ordering,
/// IDENTITY pkid, and the two N-N id lists (populated on get-by-id, re-synced on create/update).
/// </summary>
public sealed class InMemoryCourseRepository : ICourseRepository
{
    private readonly List<Course> _rows = [];
    private int _nextPkid = 1;

    private readonly Dictionary<short, string> _partners = [];
    private readonly Dictionary<short, string> _groups = [];
    private readonly Dictionary<byte, string> _statuses = [];

    public InMemoryCourseRepository WithPartners(params (short pkid, string name)[] rows)
    {
        foreach (var (pkid, name) in rows) _partners[pkid] = name;
        return this;
    }

    public InMemoryCourseRepository WithCourseGroups(params (short pkid, string description)[] rows)
    {
        foreach (var (pkid, description) in rows) _groups[pkid] = description;
        return this;
    }

    public InMemoryCourseRepository WithPublishStatuses(params (byte pkid, string description)[] rows)
    {
        foreach (var (pkid, description) in rows) _statuses[pkid] = description;
        return this;
    }

    public InMemoryCourseRepository Seed(params Course[] rows)
    {
        foreach (var r in rows)
        {
            if (r.Pkid == 0) r.Pkid = _nextPkid;
            _nextPkid = Math.Max(_nextPkid, r.Pkid) + 1;
            _rows.Add(r);
        }
        return this;
    }

    public Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(Ordered(_rows));

    public Task<IReadOnlyList<Course>> QueryAsync(CourseQuery query, CancellationToken ct = default)
    {
        IEnumerable<Course> q = _rows;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            bool Match(string? s) => s != null && s.Contains(kw, StringComparison.OrdinalIgnoreCase);
            q = q.Where(c => Match(c.Title) || Match(c.OfficialTitle) || Match(c.CourseId)
                             || Match(c.ProdCourseId) || Match(c.FriendlyUrl));
        }
        if (query.PartnerPkid.HasValue)
            q = q.Where(c => c.PartnerPkid == query.PartnerPkid.Value);
        if (query.CourseGroupPkid.HasValue)
            q = q.Where(c => c.CourseGroupPkid == query.CourseGroupPkid.Value);
        if (query.PublishStatusPkid.HasValue)
            q = q.Where(c => c.PublishStatusPkid == query.PublishStatusPkid.Value);
        if (query.ScheduleOnFrom.HasValue)
            q = q.Where(c => c.ScheduleOn >= query.ScheduleOnFrom.Value);
        if (query.ScheduleOnTo.HasValue)
            q = q.Where(c => c.ScheduleOn <= query.ScheduleOnTo.Value);
        if (query.ScheduleOffFrom.HasValue)
            q = q.Where(c => c.ScheduleOff >= query.ScheduleOffFrom.Value);
        if (query.ScheduleOffTo.HasValue)
            q = q.Where(c => c.ScheduleOff <= query.ScheduleOffTo.Value);
        if (query.CanRepeat.HasValue)
            q = q.Where(c => c.CanRepeat == query.CanRepeat.Value);

        return Task.FromResult(Ordered(q));
    }

    public Task<Course?> GetByIdAsync(int pkid, CancellationToken ct = default)
    {
        var row = _rows.FirstOrDefault(c => c.Pkid == pkid);
        return Task.FromResult(row is null ? null : Hydrate(row));
    }

    public Task<Course> CreateAsync(CourseRequest request, CancellationToken ct = default)
    {
        var row = MapScalar(new Course { Pkid = _nextPkid++ }, request);
        row.CertificationPkids = request.CertificationPkids.Distinct().ToList();
        row.JobCategoryPkids = request.JobCategoryPkids.Distinct().ToList();
        _rows.Add(row);
        return Task.FromResult(Hydrate(row));
    }

    public Task<bool> UpdateAsync(CourseRequest request, CancellationToken ct = default)
    {
        var row = _rows.FirstOrDefault(c => c.Pkid == request.Pkid);
        if (row is null) return Task.FromResult(false);

        MapScalar(row, request);
        row.CertificationPkids = request.CertificationPkids.Distinct().ToList();
        row.JobCategoryPkids = request.JobCategoryPkids.Distinct().ToList();
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int pkid, CancellationToken ct = default)
    {
        var row = _rows.FirstOrDefault(c => c.Pkid == pkid);
        if (row is null) return Task.FromResult(false);
        _rows.Remove(row);
        return Task.FromResult(true);
    }

    // --- helpers ---------------------------------------------------------

    private static Course MapScalar(Course row, CourseRequest r)
    {
        row.Title = r.Title;
        row.OfficialTitle = r.OfficialTitle;
        row.CourseId = r.CourseId;
        row.ProdCourseId = r.ProdCourseId;
        row.FriendlyUrl = r.FriendlyUrl;
        row.DisplayOrder = r.DisplayOrder;
        row.PartnerPkid = r.PartnerPkid;
        row.CourseGroupPkid = r.CourseGroupPkid;
        row.PublishStatusPkid = r.PublishStatusPkid;
        row.ScheduleOn = r.ScheduleOn;
        row.ScheduleOff = r.ScheduleOff;
        row.Hour = r.Hour;
        row.ListPrice = r.ListPrice;
        row.LearningCredit = r.LearningCredit;
        row.Material = r.Material;
        row.Objective = r.Objective;
        row.Target = r.Target;
        row.Prerequisites = r.Prerequisites;
        row.Outline = r.Outline;
        row.TowardCertOrExam = r.TowardCertOrExam;
        row.Note = r.Note;
        row.OtherInfo = r.OtherInfo;
        row.CanRepeat = r.CanRepeat;
        return row;
    }

    private Course Hydrate(Course c)
    {
        c.Partner = _partners.TryGetValue(c.PartnerPkid, out var pn)
            ? new PartnerLookup { Pkid = c.PartnerPkid, Name = pn } : null;
        c.CourseGroup = c.CourseGroupPkid.HasValue && _groups.TryGetValue(c.CourseGroupPkid.Value, out var gn)
            ? new CourseGroupLookup { Pkid = c.CourseGroupPkid.Value, Description = gn } : null;
        c.PublishStatus = _statuses.TryGetValue(c.PublishStatusPkid, out var sn)
            ? new PublishStatusLookup { Pkid = c.PublishStatusPkid, Description = sn } : null;
        return c;
    }

    private IReadOnlyList<Course> Ordered(IEnumerable<Course> src)
        => src.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Pkid).Select(Hydrate).ToList();
}
