using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IPublishStatusRepository"/> for controller tests — mirrors the SQL
/// behaviour (Description LIKE, tri-state bool matching, pkid ordering) without a database.
/// </summary>
public sealed class InMemoryPublishStatusRepository : IPublishStatusRepository
{
    private readonly List<PublishStatus> _rows = [];

    public InMemoryPublishStatusRepository Seed(params PublishStatus[] rows)
    {
        _rows.AddRange(rows);
        return this;
    }

    public Task<IReadOnlyList<PublishStatus>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(Ordered(_rows));

    public Task<IReadOnlyList<PublishStatus>> QueryAsync(PublishStatusQuery query, CancellationToken ct = default)
    {
        IEnumerable<PublishStatus> q = _rows;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(s => s.Description.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.IsDraft.HasValue)
            q = q.Where(s => s.IsDraft == query.IsDraft.Value);

        if (query.IsPublished.HasValue)
            q = q.Where(s => s.IsPublished == query.IsPublished.Value);

        if (query.IsDiscontinued.HasValue)
            q = q.Where(s => s.IsDiscontinued == query.IsDiscontinued.Value);

        return Task.FromResult(Ordered(q));
    }

    public Task<PublishStatus?> GetByIdAsync(byte pkid, CancellationToken ct = default)
        => Task.FromResult(Find(pkid));

    public Task<bool> ExistsAsync(byte pkid, CancellationToken ct = default)
        => Task.FromResult(Find(pkid) is not null);

    public Task<PublishStatus> CreateAsync(PublishStatusRequest request, CancellationToken ct = default)
    {
        var row = new PublishStatus
        {
            Pkid = request.Pkid,
            Description = request.Description,
            IsDraft = request.IsDraft,
            IsPublished = request.IsPublished,
            IsDiscontinued = request.IsDiscontinued,
        };
        _rows.Add(row);
        return Task.FromResult(row);
    }

    public Task<bool> UpdateAsync(PublishStatusRequest request, CancellationToken ct = default)
    {
        var row = Find(request.Pkid);
        if (row is null) return Task.FromResult(false);

        row.Description = request.Description;
        row.IsDraft = request.IsDraft;
        row.IsPublished = request.IsPublished;
        row.IsDiscontinued = request.IsDiscontinued;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(byte pkid, CancellationToken ct = default)
    {
        var row = Find(pkid);
        if (row is null) return Task.FromResult(false);
        _rows.Remove(row);
        return Task.FromResult(true);
    }

    private PublishStatus? Find(byte pkid) => _rows.FirstOrDefault(s => s.Pkid == pkid);

    private static IReadOnlyList<PublishStatus> Ordered(IEnumerable<PublishStatus> src)
        => src.OrderBy(s => s.Pkid).ToList();
}
