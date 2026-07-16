using System.Data;
using System.Security.Claims;
using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Repositories;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end proof that a retrofitted repository writes the right RowAudit row on each path, and none
/// when the change fails. Runs the REAL <see cref="PublishStatusRepository"/> + REAL
/// <see cref="RowAuditWriter"/> against an in-memory SQLite database (a natural-key table with no
/// SQL-Server-only syntax), so the audit insert really happens on the operation's connection/transaction.
/// </summary>
public sealed class PublishStatusRepositoryAuditTests : IDisposable
{
    private readonly SqliteConnection _keepAlive; // holds the shared in-memory DB open for the test
    private readonly SqliteConnectionFactory _factory;
    private readonly PublishStatusRepository _repo;

    public PublishStatusRepositoryAuditTests()
    {
        // Unique shared-cache in-memory DB per test instance (xUnit news up one per test → isolation).
        var dataSource = $"audit-{Guid.NewGuid():N}";
        var connectionString = $"Data Source={dataSource};Mode=Memory;Cache=Shared";

        _keepAlive = new SqliteConnection(connectionString);
        _keepAlive.Open();
        CreateSchema(_keepAlive);

        _factory = new SqliteConnectionFactory(connectionString);
        _repo = new PublishStatusRepository(_factory, WriterFor("alice"));
    }

    // --- Tests ------------------------------------------------------------------------------------

    [Fact]
    public async Task Create_writes_one_Insert_audit_row_with_the_first_string_column()
    {
        await _repo.CreateAsync(Req(10, "草稿 Draft", isDraft: true), default);

        var audits = await ReadAuditsAsync();
        var row = Assert.Single(audits);
        Assert.Equal("PublishStatus", row.TableName);
        Assert.Equal("Insert", row.ActionType);
        Assert.Equal("10", row.PrimaryKeyValues);
        Assert.Equal("草稿 Draft", row.ActionDesc);   // Description = first string property
        Assert.Equal("alice", row.UserName);
    }

    [Fact]
    public async Task Update_writes_an_Update_audit_row_listing_exactly_the_changed_columns()
    {
        await _repo.CreateAsync(Req(20, "Before", isDraft: true, isPublished: false), default);

        // Change Description and IsPublished only; IsDraft/IsDiscontinued unchanged.
        var ok = await _repo.UpdateAsync(Req(20, "After", isDraft: true, isPublished: true), default);
        Assert.True(ok);

        var update = Assert.Single(await ReadAuditsAsync("Update"));
        Assert.Equal("20", update.PrimaryKeyValues);
        Assert.Equal("Description, IsPublished", update.ActionDesc);
    }

    [Fact]
    public async Task Update_with_no_actual_change_writes_no_audit_row()
    {
        await _repo.CreateAsync(Req(25, "Same", isDraft: true), default);

        await _repo.UpdateAsync(Req(25, "Same", isDraft: true), default); // identical values

        Assert.Empty(await ReadAuditsAsync("Update"));
    }

    [Fact]
    public async Task Delete_writes_a_Delete_audit_row()
    {
        await _repo.CreateAsync(Req(30, "Doomed"), default);

        var ok = await _repo.DeleteAsync(30, default);
        Assert.True(ok);

        var del = Assert.Single(await ReadAuditsAsync("Delete"));
        Assert.Equal("30", del.PrimaryKeyValues);
        Assert.Equal("Doomed", del.ActionDesc);   // first string property of the deleted row
    }

    [Fact]
    public async Task Failed_change_leaves_no_audit_row()
    {
        // Updating / deleting a row that does not exist changes nothing → no audit row at all.
        var updated = await _repo.UpdateAsync(Req(199, "ghost"), default);
        var deleted = await _repo.DeleteAsync(198, default);

        Assert.False(updated);
        Assert.False(deleted);
        Assert.Empty(await ReadAuditsAsync());
    }

    // --- Helpers ----------------------------------------------------------------------------------

    private static PublishStatusRequest Req(
        byte pkid, string description, bool isDraft = false, bool isPublished = false, bool isDiscontinued = false)
        => new()
        {
            Pkid = pkid,
            Description = description,
            IsDraft = isDraft,
            IsPublished = isPublished,
            IsDiscontinued = isDiscontinued,
        };

    private static RowAuditWriter WriterFor(string userName)
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim("userName", userName) }, authenticationType: "test")),
            },
        };
        return new RowAuditWriter(accessor);
    }

    private async Task<IReadOnlyList<AuditRow>> ReadAuditsAsync(string? actionType = null)
    {
        using var conn = await _factory.CreateOpenConnectionAsync();
        var where = actionType is null ? "" : " WHERE ActionType = @actionType";
        var rows = await conn.QueryAsync<AuditRow>(
            $@"SELECT TableName, UserName, PrimaryKeyValues, ActionType, ActionDesc
               FROM RowAudit{where} ORDER BY pkid",
            new { actionType });
        return rows.AsList();
    }

    private static void CreateSchema(IDbConnection conn)
    {
        conn.Execute(
            @"CREATE TABLE PublishStatus (
                  pkid            INTEGER PRIMARY KEY,
                  Description     TEXT    NOT NULL,
                  IsDraft         INTEGER NOT NULL,
                  IsPublished     INTEGER NOT NULL,
                  IsDiscontinued  INTEGER NOT NULL
              );
              CREATE TABLE RowAudit (
                  pkid              INTEGER PRIMARY KEY AUTOINCREMENT,
                  TableName         TEXT    NOT NULL,
                  UserName          TEXT    NOT NULL,
                  PrimaryKeyValues  TEXT    NOT NULL,
                  ActionType        TEXT    NOT NULL,
                  ActionDesc        TEXT    NULL,
                  [DateTime]        TEXT    NOT NULL
              );");
    }

    public void Dispose() => _keepAlive.Dispose();

    private sealed class AuditRow
    {
        public string TableName { get; set; } = "";
        public string UserName { get; set; } = "";
        public string PrimaryKeyValues { get; set; } = "";
        public string ActionType { get; set; } = "";
        public string? ActionDesc { get; set; }
    }

    /// <summary>Test <see cref="IDbConnectionFactory"/> that opens SQLite connections to a shared DB.</summary>
    private sealed class SqliteConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        public SqliteConnectionFactory(string connectionString) => _connectionString = connectionString;

        public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
        {
            var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(ct);
            return conn;
        }
    }
}
