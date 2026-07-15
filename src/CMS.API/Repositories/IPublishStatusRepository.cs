using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IPublishStatusRepository
{
    Task<IReadOnlyList<PublishStatus>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PublishStatus>> QueryAsync(PublishStatusQuery query, CancellationToken ct = default);
    Task<PublishStatus?> GetByIdAsync(byte pkid, CancellationToken ct = default);
    Task<bool> ExistsAsync(byte pkid, CancellationToken ct = default);

    /// <summary>Inserts the status (pkid supplied by caller). Returns the created record.</summary>
    Task<PublishStatus> CreateAsync(PublishStatusRequest request, CancellationToken ct = default);

    /// <summary>Updates the status (pkid immutable). Returns false if it does not exist.</summary>
    Task<bool> UpdateAsync(PublishStatusRequest request, CancellationToken ct = default);

    Task<bool> DeleteAsync(byte pkid, CancellationToken ct = default);
}
