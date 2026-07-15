using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="ICourseGroupRepository"/> for controller tests — mirrors the SQL behaviour
/// (Description LIKE, pkid ordering, IDENTITY pkid) without a database.
/// </summary>
public sealed class InMemoryCourseGroupRepository : ICourseGroupRepository
{
    private readonly List<CourseGroup> _rows = [];
    private short _nextPkid = 1;

    public InMemoryCourseGroupRepository Seed(params CourseGroup[] rows)
    {
        foreach (var r in rows)
        {
            if (r.Pkid == 0) r.Pkid = _nextPkid;
            _nextPkid = (short)(Math.Max(_nextPkid, r.Pkid) + 1);
            _rows.Add(r);
        }
        return this;
    }

    public Task<IReadOnlyList<CourseGroup>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(Ordered(_rows));

    public Task<IReadOnlyList<CourseGroup>> QueryAsync(CourseGroupQuery query, CancellationToken ct = default)
    {
        IEnumerable<CourseGroup> q = _rows;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(g => g.Description.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(Ordered(q));
    }

    public Task<CourseGroup?> GetByIdAsync(short pkid, CancellationToken ct = default)
        => Task.FromResult(Find(pkid));

    public Task<CourseGroup> CreateAsync(CourseGroupRequest request, CancellationToken ct = default)
    {
        var row = new CourseGroup { Pkid = _nextPkid++, Description = request.Description };
        _rows.Add(row);
        return Task.FromResult(row);
    }

    public Task<bool> UpdateAsync(CourseGroupRequest request, CancellationToken ct = default)
    {
        var row = Find(request.Pkid);
        if (row is null) return Task.FromResult(false);

        row.Description = request.Description;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(short pkid, CancellationToken ct = default)
    {
        var row = Find(pkid);
        if (row is null) return Task.FromResult(false);
        _rows.Remove(row);
        return Task.FromResult(true);
    }

    private CourseGroup? Find(short pkid) => _rows.FirstOrDefault(g => g.Pkid == pkid);

    private static IReadOnlyList<CourseGroup> Ordered(IEnumerable<CourseGroup> src)
        => src.OrderBy(g => g.Pkid).ToList();
}
