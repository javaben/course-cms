using System.Data;

namespace CMS.API.Infrastructure;

/// <summary>Creates open ADO.NET connections for Dapper.</summary>
public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
