using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class LookupRepository : ILookupRepository
{
    private readonly IDbConnectionFactory _factory;

    public LookupRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<AppUserLookup>> GetAppUsersAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<AppUserLookup>(new CommandDefinition(
            @"SELECT UserId, UserName
              FROM AppUser
              ORDER BY IsActive DESC, UserName ASC",
            cancellationToken: ct));
        return rows.AsList();
    }
}
