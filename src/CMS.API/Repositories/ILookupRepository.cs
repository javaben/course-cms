using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ILookupRepository
{
    /// <summary>AppUser lookup rows for the AppRole n-n picker (active users first).</summary>
    Task<IReadOnlyList<AppUserLookup>> GetAppUsersAsync(CancellationToken ct = default);
}
