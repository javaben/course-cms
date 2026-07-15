using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IPartnerRepository
{
    Task<IReadOnlyList<Partner>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Partner>> QueryAsync(PartnerQuery query, CancellationToken ct = default);
    Task<Partner?> GetByIdAsync(short pkid, CancellationToken ct = default);

    /// <summary>Inserts the partner (pkid auto-generated). Returns the created record.</summary>
    Task<Partner> CreateAsync(PartnerRequest request, CancellationToken ct = default);

    /// <summary>Updates the partner (pkid immutable). Returns false if it does not exist.</summary>
    Task<bool> UpdateAsync(PartnerRequest request, CancellationToken ct = default);

    Task<bool> DeleteAsync(short pkid, CancellationToken ct = default);
}
