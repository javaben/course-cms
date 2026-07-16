using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>
/// Dapper repository for Course (課程) — the first feature with outbound FK nav joins (multi-map)
/// and two N-N junctions (delete-then-reinsert inside a transaction).
/// </summary>
public sealed class CourseRepository : ICourseRepository
{
    private const string TableName = "Course";

    private readonly IDbConnectionFactory _factory;
    private readonly IRowAuditWriter _audit;

    public CourseRepository(IDbConnectionFactory factory, IRowAuditWriter audit)
    {
        _factory = factory;
        _audit = audit;
    }

    // Course scalar columns + the three FK nav objects (each nav's leading column AS Pkid → splitOn).
    private const string SelectColumns = @"
        SELECT c.pkid                AS Pkid,
               c.Title               AS Title,
               c.OfficialTitle       AS OfficialTitle,
               c.CourseId            AS CourseId,
               c.ProdCourseId        AS ProdCourseId,
               c.FriendlyUrl         AS FriendlyUrl,
               c.DisplayOrder        AS DisplayOrder,
               c.Partner_pkid        AS PartnerPkid,
               c.CourseGroup_pkid    AS CourseGroupPkid,
               c.PublishStatus_pkid  AS PublishStatusPkid,
               c.ScheduleOn          AS ScheduleOn,
               c.ScheduleOff         AS ScheduleOff,
               c.Hour                AS Hour,
               c.ListPrice           AS ListPrice,
               c.LearningCredit      AS LearningCredit,
               c.Material            AS Material,
               c.Objective           AS Objective,
               c.Target              AS Target,
               c.Prerequisites       AS Prerequisites,
               c.Outline             AS Outline,
               c.TowardCertOrExam    AS TowardCertOrExam,
               c.Note                AS Note,
               c.OtherInfo           AS OtherInfo,
               c.CanRepeat           AS CanRepeat,
               p.pkid  AS Pkid, p.Name        AS Name,          -- Partner nav
               g.pkid  AS Pkid, g.Description  AS Description,   -- CourseGroup nav
               s.pkid  AS Pkid, s.Description  AS Description    -- PublishStatus nav
        FROM Course c
        LEFT JOIN Partner       p ON p.pkid = c.Partner_pkid
        LEFT JOIN CourseGroup   g ON g.pkid = c.CourseGroup_pkid
        LEFT JOIN PublishStatus s ON s.pkid = c.PublishStatus_pkid";

    private static async Task<List<Course>> QueryCoursesAsync(
        IDbConnection conn, string sql, object? param, CancellationToken ct, IDbTransaction? tx = null)
    {
        var rows = await conn.QueryAsync<Course, PartnerLookup, CourseGroupLookup, PublishStatusLookup, Course>(
            new CommandDefinition(sql, param, tx, cancellationToken: ct),
            (course, partner, group, status) =>
            {
                course.Partner = partner;
                course.CourseGroup = group;
                course.PublishStatus = status;
                return course;
            },
            splitOn: "Pkid,Pkid,Pkid");
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY c.DisplayOrder ASC, c.pkid ASC";
        return await QueryCoursesAsync(conn, sql, null, ct);
    }

    public async Task<IReadOnlyList<Course>> QueryAsync(CourseQuery query, CancellationToken ct = default)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add(@"(c.Title LIKE @Keyword
                        OR c.OfficialTitle LIKE @Keyword
                        OR c.CourseId LIKE @Keyword
                        OR c.ProdCourseId LIKE @Keyword
                        OR c.FriendlyUrl LIKE @Keyword)");
            p.Add("@Keyword", $"%{query.Keyword.Trim()}%");
        }
        if (query.PartnerPkid.HasValue)
        {
            where.Add("c.Partner_pkid = @PartnerPkid");
            p.Add("@PartnerPkid", query.PartnerPkid.Value);
        }
        if (query.CourseGroupPkid.HasValue)
        {
            where.Add("c.CourseGroup_pkid = @CourseGroupPkid");
            p.Add("@CourseGroupPkid", query.CourseGroupPkid.Value);
        }
        if (query.PublishStatusPkid.HasValue)
        {
            where.Add("c.PublishStatus_pkid = @PublishStatusPkid");
            p.Add("@PublishStatusPkid", query.PublishStatusPkid.Value);
        }
        if (query.ScheduleOnFrom.HasValue)
        {
            where.Add("c.ScheduleOn >= @ScheduleOnFrom");
            p.Add("@ScheduleOnFrom", query.ScheduleOnFrom.Value);
        }
        if (query.ScheduleOnTo.HasValue)
        {
            where.Add("c.ScheduleOn <= @ScheduleOnTo");
            p.Add("@ScheduleOnTo", query.ScheduleOnTo.Value);
        }
        if (query.ScheduleOffFrom.HasValue)
        {
            where.Add("c.ScheduleOff >= @ScheduleOffFrom");
            p.Add("@ScheduleOffFrom", query.ScheduleOffFrom.Value);
        }
        if (query.ScheduleOffTo.HasValue)
        {
            where.Add("c.ScheduleOff <= @ScheduleOffTo");
            p.Add("@ScheduleOffTo", query.ScheduleOffTo.Value);
        }
        if (query.CanRepeat.HasValue)
        {
            where.Add("c.CanRepeat = @CanRepeat");
            p.Add("@CanRepeat", query.CanRepeat.Value);
        }

        var whereClause = where.Count > 0 ? $" WHERE {string.Join(" AND ", where)}" : "";
        var sql = $"{SelectColumns}{whereClause} ORDER BY c.DisplayOrder ASC, c.pkid ASC";

        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await QueryCoursesAsync(conn, sql, p, ct);
    }

    public async Task<Course?> GetByIdAsync(int pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await LoadAsync(conn, null, pkid, ct);
    }

    /// <summary>
    /// Loads one course (FK nav objects + both N-N id lists) on the given connection/transaction —
    /// used to snapshot the row before/after a change for the audit log.
    /// </summary>
    private static async Task<Course?> LoadAsync(
        IDbConnection conn, IDbTransaction? tx, int pkid, CancellationToken ct)
    {
        var sql = $"{SelectColumns} WHERE c.pkid = @Pkid";
        var rows = await QueryCoursesAsync(conn, sql, new { Pkid = pkid }, ct, tx);
        var course = rows.FirstOrDefault();
        if (course is null) return null;

        await LoadJunctionsAsync(conn, tx, course, ct);
        return course;
    }

    public async Task<Course> CreateAsync(CourseRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var newId = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"INSERT INTO Course (Title, OfficialTitle, CourseId, ProdCourseId, FriendlyUrl, DisplayOrder,
                  Partner_pkid, CourseGroup_pkid, PublishStatus_pkid, ScheduleOn, ScheduleOff, Hour,
                  ListPrice, LearningCredit, Material, Objective, Target, Prerequisites, Outline,
                  TowardCertOrExam, Note, OtherInfo, CanRepeat)
              VALUES (@Title, @OfficialTitle, @CourseId, @ProdCourseId, @FriendlyUrl, @DisplayOrder,
                  @PartnerPkid, @CourseGroupPkid, @PublishStatusPkid, @ScheduleOn, @ScheduleOff, @Hour,
                  @ListPrice, @LearningCredit, @Material, @Objective, @Target, @Prerequisites, @Outline,
                  @TowardCertOrExam, @Note, @OtherInfo, @CanRepeat);
              SELECT CAST(SCOPE_IDENTITY() AS int);",
            request, tx, cancellationToken: ct));

        await SyncJunctionsAsync(conn, tx, newId, request, ct);

        var created = (await LoadAsync(conn, tx, newId, ct))!;
        await _audit.LogInsertAsync(conn, tx, TableName, created, ct);

        tx.Commit();
        return created;
    }

    public async Task<bool> UpdateAsync(CourseRequest request, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var before = await LoadAsync(conn, tx, request.Pkid, ct);
        if (before is null) { tx.Rollback(); return false; }

        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE Course
                 SET Title=@Title, OfficialTitle=@OfficialTitle, CourseId=@CourseId,
                     ProdCourseId=@ProdCourseId, FriendlyUrl=@FriendlyUrl, DisplayOrder=@DisplayOrder,
                     Partner_pkid=@PartnerPkid, CourseGroup_pkid=@CourseGroupPkid,
                     PublishStatus_pkid=@PublishStatusPkid, ScheduleOn=@ScheduleOn, ScheduleOff=@ScheduleOff,
                     Hour=@Hour, ListPrice=@ListPrice, LearningCredit=@LearningCredit, Material=@Material,
                     Objective=@Objective, Target=@Target, Prerequisites=@Prerequisites, Outline=@Outline,
                     TowardCertOrExam=@TowardCertOrExam, Note=@Note, OtherInfo=@OtherInfo, CanRepeat=@CanRepeat
               WHERE pkid=@Pkid;",
            request, tx, cancellationToken: ct));

        await SyncJunctionsAsync(conn, tx, request.Pkid, request, ct);

        var after = (await LoadAsync(conn, tx, request.Pkid, ct))!;
        await _audit.LogUpdateAsync(conn, tx, TableName, before, after, ct);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(int pkid, CancellationToken ct = default)
    {
        using var conn = await _factory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var before = await LoadAsync(conn, tx, pkid, ct);
        if (before is null) { tx.Rollback(); return false; }

        // Remove junction rows first, then the course (FK-safe even without ON DELETE CASCADE).
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM CourseInCertification WHERE Course_pkid=@Pkid;" +
            "DELETE FROM CourseJobCategories WHERE Course_pkid=@Pkid;",
            new { Pkid = pkid }, tx, cancellationToken: ct));

        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Course WHERE pkid=@Pkid",
            new { Pkid = pkid }, tx, cancellationToken: ct));

        await _audit.LogDeleteAsync(conn, tx, TableName, before, ct);

        tx.Commit();
        return true;
    }

    /// <summary>Fill the two N-N id lists for a loaded course (same connection; optional tx).</summary>
    private static async Task LoadJunctionsAsync(
        IDbConnection conn, IDbTransaction? tx, Course course, CancellationToken ct)
    {
        var certIds = await conn.QueryAsync<int>(new CommandDefinition(
            "SELECT Certification_pkid FROM CourseInCertification WHERE Course_pkid=@Pkid ORDER BY Certification_pkid",
            new { Pkid = course.Pkid }, tx, cancellationToken: ct));
        course.CertificationPkids = certIds.AsList();

        var jobIds = await conn.QueryAsync<short>(new CommandDefinition(
            "SELECT JobCategory_pkid FROM CourseJobCategories WHERE Course_pkid=@Pkid ORDER BY JobCategory_pkid",
            new { Pkid = course.Pkid }, tx, cancellationToken: ct));
        course.JobCategoryPkids = jobIds.AsList();
    }

    /// <summary>n-n sync: delete-then-reinsert both junctions for the course, inside the given transaction.</summary>
    private static async Task SyncJunctionsAsync(
        IDbConnection conn, IDbTransaction tx, int coursePkid, CourseRequest request, CancellationToken ct)
    {
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM CourseInCertification WHERE Course_pkid=@Pkid",
            new { Pkid = coursePkid }, tx, cancellationToken: ct));

        var certIds = request.CertificationPkids.Distinct().ToList();
        if (certIds.Count > 0)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO CourseInCertification (Course_pkid, Certification_pkid) VALUES (@Pkid, @CertId)",
                certIds.Select(id => new { Pkid = coursePkid, CertId = id }), tx, cancellationToken: ct));
        }

        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM CourseJobCategories WHERE Course_pkid=@Pkid",
            new { Pkid = coursePkid }, tx, cancellationToken: ct));

        var jobIds = request.JobCategoryPkids.Distinct().ToList();
        if (jobIds.Count > 0)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO CourseJobCategories (Course_pkid, JobCategory_pkid) VALUES (@Pkid, @JobId)",
                jobIds.Select(id => new { Pkid = coursePkid, JobId = id }), tx, cancellationToken: ct));
        }
    }
}
