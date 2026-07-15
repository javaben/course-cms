using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IPartnerRepository"/> for controller tests — mirrors the SQL behaviour
/// (keyword LIKE across name columns, DisplayOrder ordering, IDENTITY pkid) without a database.
/// </summary>
public sealed class InMemoryPartnerRepository : IPartnerRepository
{
    private readonly List<Partner> _rows = [];
    private short _nextPkid = 1;

    public InMemoryPartnerRepository Seed(params Partner[] rows)
    {
        foreach (var r in rows)
        {
            if (r.Pkid == 0) r.Pkid = _nextPkid;
            _nextPkid = (short)(Math.Max(_nextPkid, r.Pkid) + 1);
            _rows.Add(r);
        }
        return this;
    }

    public Task<IReadOnlyList<Partner>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(Ordered(_rows));

    public Task<IReadOnlyList<Partner>> QueryAsync(PartnerQuery query, CancellationToken ct = default)
    {
        IEnumerable<Partner> q = _rows;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(p =>
                p.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                p.AppKey.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                p.NameOnPartnerMenu.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                p.NameOnCourseDetailPage.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(Ordered(q));
    }

    public Task<Partner?> GetByIdAsync(short pkid, CancellationToken ct = default)
        => Task.FromResult(Find(pkid));

    public Task<Partner> CreateAsync(PartnerRequest request, CancellationToken ct = default)
    {
        var row = new Partner
        {
            Pkid = _nextPkid++,
            Name = request.Name,
            AppKey = request.AppKey,
            NameOnPartnerMenu = request.NameOnPartnerMenu,
            NameOnCourseDetailPage = request.NameOnCourseDetailPage,
            DisplayOrder = request.DisplayOrder,
            ImageFilename = request.ImageFilename,
        };
        _rows.Add(row);
        return Task.FromResult(row);
    }

    public Task<bool> UpdateAsync(PartnerRequest request, CancellationToken ct = default)
    {
        var row = Find(request.Pkid);
        if (row is null) return Task.FromResult(false);

        row.Name = request.Name;
        row.AppKey = request.AppKey;
        row.NameOnPartnerMenu = request.NameOnPartnerMenu;
        row.NameOnCourseDetailPage = request.NameOnCourseDetailPage;
        row.DisplayOrder = request.DisplayOrder;
        row.ImageFilename = request.ImageFilename;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(short pkid, CancellationToken ct = default)
    {
        var row = Find(pkid);
        if (row is null) return Task.FromResult(false);
        _rows.Remove(row);
        return Task.FromResult(true);
    }

    private Partner? Find(short pkid) => _rows.FirstOrDefault(p => p.Pkid == pkid);

    private static IReadOnlyList<Partner> Ordered(IEnumerable<Partner> src)
        => src.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Pkid).ToList();
}
