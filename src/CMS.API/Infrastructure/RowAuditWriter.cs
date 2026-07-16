using System.Data;
using System.Reflection;
using CMS.API.Models;
using Dapper;
using Microsoft.AspNetCore.Http;

namespace CMS.API.Infrastructure;

/// <summary>
/// Cross-cutting audit writer. A repository calls one of these after an Insert / Update / Delete and
/// exactly one <c>RowAudit</c> row is written describing the change. Works for any entity type via
/// reflection — no per-table code.
/// <para>
/// The audit row is written on the <b>same connection/transaction</b> as the change (both are passed
/// in) so a rolled-back or failed change leaves no audit row behind.
/// </para>
/// </summary>
public interface IRowAuditWriter
{
    /// <summary>Log an insert. ActionDesc = the entity's first string property (e.g. Name/Title).</summary>
    Task LogInsertAsync(IDbConnection conn, IDbTransaction? tx, string tableName, object entity, CancellationToken ct = default);

    /// <summary>
    /// Log an update. ActionDesc = a comma-separated list of the (scalar) property names whose value
    /// changed between <paramref name="before"/> and <paramref name="after"/>. When nothing changed
    /// no row is written.
    /// </summary>
    Task LogUpdateAsync(IDbConnection conn, IDbTransaction? tx, string tableName, object before, object after, CancellationToken ct = default);

    /// <summary>Log a delete. ActionDesc = the deleted entity's first string property.</summary>
    Task LogDeleteAsync(IDbConnection conn, IDbTransaction? tx, string tableName, object entity, CancellationToken ct = default);
}

public sealed class RowAuditWriter : IRowAuditWriter
{
    /// <summary>ActionDesc is capped to the RowAudit.ActionDesc column width.</summary>
    public const int MaxActionDescLength = 1000;

    /// <summary>UserName written when there is no authenticated user on the request.</summary>
    public const string UserNameFallback = "system";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public RowAuditWriter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task LogInsertAsync(IDbConnection conn, IDbTransaction? tx, string tableName, object entity, CancellationToken ct = default)
        => InsertAsync(conn, tx, BuildInsertAudit(tableName, entity), ct);

    public Task LogUpdateAsync(IDbConnection conn, IDbTransaction? tx, string tableName, object before, object after, CancellationToken ct = default)
    {
        var row = BuildUpdateAudit(tableName, before, after);
        // Nothing changed → skip the row entirely (ActionDesc would be empty).
        return string.IsNullOrEmpty(row.ActionDesc) ? Task.CompletedTask : InsertAsync(conn, tx, row, ct);
    }

    public Task LogDeleteAsync(IDbConnection conn, IDbTransaction? tx, string tableName, object entity, CancellationToken ct = default)
        => InsertAsync(conn, tx, BuildDeleteAudit(tableName, entity), ct);

    // --- Reflection: build the RowAudit row (pure apart from UserName + clock; no DB) --------------
    // These are public so the reflection rules can be unit-tested without a database.

    /// <summary>Builds the Insert audit row: ActionDesc = first string property value.</summary>
    public RowAudit BuildInsertAudit(string tableName, object entity)
        => NewRow(tableName, entity, "Insert", FirstStringPropertyValue(entity));

    /// <summary>
    /// Builds the Delete audit row: ActionDesc = first string property value of the deleted entity.
    /// </summary>
    public RowAudit BuildDeleteAudit(string tableName, object entity)
        => NewRow(tableName, entity, "Delete", FirstStringPropertyValue(entity));

    /// <summary>
    /// Builds the Update audit row: ActionDesc = comma-separated names of the (scalar) properties whose
    /// value differs between <paramref name="before"/> and <paramref name="after"/> (empty string when
    /// nothing changed). The pkid is read from <paramref name="after"/>.
    /// </summary>
    public RowAudit BuildUpdateAudit(string tableName, object before, object after)
        => NewRow(tableName, after, "Update", string.Join(", ", ChangedPropertyNames(before, after)));

    private RowAudit NewRow(string tableName, object entity, string actionType, string? actionDesc) => new()
    {
        TableName = tableName,
        UserName = ResolveUserName(),
        PrimaryKeyValues = PrimaryKeyValue(entity),
        ActionType = actionType,
        ActionDesc = Truncate(actionDesc),
        DateTime = DateTime.Now,
    };

    /// <summary>The signed-in user's UserName from the current request's JWT, or "system".</summary>
    private string ResolveUserName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        // NameClaimType is "userName" (Program.cs), so both the explicit claim and Identity.Name work.
        var name = user?.FindFirst("userName")?.Value ?? user?.Identity?.Name;
        return string.IsNullOrWhiteSpace(name) ? UserNameFallback : name;
    }

    /// <summary>Reads the entity's <c>pkid</c> property (case-insensitive) as a string.</summary>
    private static string PrimaryKeyValue(object entity)
    {
        var pk = OrderedProperties(entity.GetType())
            .FirstOrDefault(p => string.Equals(p.Name, "pkid", StringComparison.OrdinalIgnoreCase));
        return pk?.GetValue(entity)?.ToString() ?? "";
    }

    /// <summary>The value of the first <see cref="string"/> property in declaration order, or null.</summary>
    private static string? FirstStringPropertyValue(object entity)
    {
        var prop = OrderedProperties(entity.GetType())
            .FirstOrDefault(p => p.PropertyType == typeof(string));
        return prop?.GetValue(entity) as string;
    }

    /// <summary>
    /// Names of the (scalar) properties whose value differs, in declaration order. Only column-like
    /// scalar properties are compared — navigation objects and collections (e.g. FK lookups, n-n id
    /// lists) are not table columns and are ignored.
    /// </summary>
    private static IEnumerable<string> ChangedPropertyNames(object before, object after)
    {
        // Compare against the AFTER type's properties (before/after are the same entity type).
        foreach (var prop in OrderedProperties(after.GetType()))
        {
            if (!IsScalar(prop.PropertyType)) continue;
            if (!Equals(prop.GetValue(before), prop.GetValue(after)))
                yield return prop.Name;
        }
    }

    // Reflection returns a single type's properties in metadata-token order, which matches source
    // declaration order — sort by it explicitly so "first" / "declaration order" are well-defined.
    private static IEnumerable<PropertyInfo> OrderedProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
               .OrderBy(p => p.MetadataToken);

    /// <summary>A "column-like" value type: primitives, enums, string, and the common value structs.</summary>
    private static bool IsScalar(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t.IsEnum
            || t == typeof(string) || t == typeof(decimal)
            || t == typeof(DateTime) || t == typeof(DateTimeOffset)
            || t == typeof(DateOnly) || t == typeof(TimeOnly)
            || t == typeof(TimeSpan) || t == typeof(Guid);
    }

    private static string? Truncate(string? value)
        => value is { Length: > MaxActionDescLength } ? value[..MaxActionDescLength] : value;

    private static async Task InsertAsync(IDbConnection conn, IDbTransaction? tx, RowAudit row, CancellationToken ct)
    {
        // pkid is IDENTITY — never inserted. [DateTime] is bracketed (reserved word).
        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO RowAudit (TableName, UserName, PrimaryKeyValues, ActionType, ActionDesc, [DateTime])
              VALUES (@TableName, @UserName, @PrimaryKeyValues, @ActionType, @ActionDesc, @DateTime);",
            row, transaction: tx, cancellationToken: ct));
    }
}
