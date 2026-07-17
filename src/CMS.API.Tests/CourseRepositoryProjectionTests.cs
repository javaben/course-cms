using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Repositories;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CMS.API.Tests;

/// <summary>
/// Regression: ISSUE-001 — the Course list shipped every nvarchar(max) free-text column (Outline,
/// Objective, Note, ...) on every row, so /courses transferred 4.1 MB to render 20 rows.
/// Found by /qa on 2026-07-17.
/// Report: .gstack/qa-reports/qa-report-localhost-2026-07-17.md
///
/// The list projection and the single-row projection are now deliberately different, and that split is
/// only safe while BOTH halves hold: the list must stay lean, and GetById must stay complete (the
/// detail page, the PDF flyer and the RowAudit before/after snapshot all read the free text from it).
/// A future edit that "tidies up" the two SELECTs back into one would silently undo the fix, or worse,
/// silently blank the audit trail. These tests pin both directions.
///
/// Runs the REAL <see cref="CourseRepository"/> against in-memory SQLite, so the assertions are about
/// the SQL that actually ships — an in-memory fake repository could not catch a projection change.
/// </summary>
public sealed class CourseRepositoryProjectionTests : IDisposable
{
    private const string Outline = "<h2>課程大綱</h2><p>Day 1 ...</p>";
    private const string Objective = "<p>課程目標</p>";

    private readonly SqliteConnection _keepAlive;
    private readonly CourseRepository _repo;

    static CourseRepositoryProjectionTests()
    {
        // SQLite has no date type, so Course.ScheduleOn/Off come back as TEXT and Dapper cannot cast
        // them to DateOnly. SQL Server maps `date` → DateOnly natively, so this handler is a harness
        // detail and never runs in production.
        SqlMapper.AddTypeHandler(new SqliteDateOnlyHandler());
    }

    public CourseRepositoryProjectionTests()
    {
        var connectionString = $"Data Source=course-proj-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _keepAlive = new SqliteConnection(connectionString);
        _keepAlive.Open();
        CreateSchema(_keepAlive);
        SeedCourse(_keepAlive);

        _repo = new CourseRepository(new SqliteConnectionFactory(connectionString), new NoOpRowAuditWriter());
    }

    [Fact]
    public async Task QueryAsync_omits_the_free_text_columns()
    {
        var rows = await _repo.QueryAsync(new CourseQuery());

        var course = Assert.Single(rows);
        Assert.Null(course.Outline);
        Assert.Null(course.Objective);
        Assert.Null(course.Note);
        Assert.Null(course.Target);
        Assert.Null(course.Prerequisites);
        Assert.Null(course.Material);
        Assert.Null(course.TowardCertOrExam);
        Assert.Null(course.OtherInfo);
    }

    [Fact]
    public async Task GetAllAsync_omits_the_free_text_columns()
    {
        var rows = await _repo.GetAllAsync();

        Assert.Null(Assert.Single(rows).Outline);
    }

    [Fact]
    public async Task QueryAsync_still_returns_every_column_the_list_renders()
    {
        var rows = await _repo.QueryAsync(new CourseQuery());

        // Trimming the projection must not cost the list any column it actually draws, exports or
        // filters on — that is the failure mode of cutting one column too many.
        var course = Assert.Single(rows);
        Assert.Equal(62, course.Pkid);
        Assert.Equal("網路基礎架構與網路服務", course.Title);
        Assert.Equal("NINS", course.CourseId);
        Assert.Equal("NINS", course.ProdCourseId);
        Assert.Equal(42, course.Hour);
        Assert.Equal(16000, course.ListPrice);
        Assert.Equal(4.0m, course.LearningCredit);
        Assert.False(course.CanRepeat);
        Assert.Equal("恆逸", course.Partner?.Name);
        Assert.Equal("IT 必修", course.CourseGroup?.Description);
        Assert.Equal("草稿", course.PublishStatus?.Description);
    }

    [Fact]
    public async Task GetByIdAsync_still_returns_the_free_text_columns()
    {
        // The detail page, the PDF flyer and the audit snapshot all read free text from this path.
        var course = await _repo.GetByIdAsync(62);

        Assert.NotNull(course);
        Assert.Equal(Outline, course!.Outline);
        Assert.Equal(Objective, course.Objective);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_an_unknown_pkid()
    {
        Assert.Null(await _repo.GetByIdAsync(999));
    }

    public void Dispose() => _keepAlive.Dispose();

    private static void CreateSchema(IDbConnection conn) => conn.Execute(
        @"CREATE TABLE Partner       (pkid INTEGER PRIMARY KEY, Name TEXT NOT NULL);
          CREATE TABLE CourseGroup   (pkid INTEGER PRIMARY KEY, Description TEXT NOT NULL);
          CREATE TABLE PublishStatus (pkid INTEGER PRIMARY KEY, Description TEXT NOT NULL);
          CREATE TABLE Course (
              pkid               INTEGER PRIMARY KEY,
              Title              TEXT    NOT NULL,
              OfficialTitle      TEXT    NULL,
              CourseId           TEXT    NOT NULL,
              ProdCourseId       TEXT    NOT NULL,
              FriendlyUrl        TEXT    NULL,
              DisplayOrder       INTEGER NOT NULL,
              Partner_pkid       INTEGER NULL,
              CourseGroup_pkid   INTEGER NULL,
              PublishStatus_pkid INTEGER NULL,
              ScheduleOn         TEXT    NULL,
              ScheduleOff        TEXT    NULL,
              Hour               INTEGER NULL,
              ListPrice          INTEGER NULL,
              LearningCredit     REAL    NULL,
              Material           TEXT    NULL,
              Objective          TEXT    NULL,
              Target             TEXT    NULL,
              Prerequisites      TEXT    NULL,
              Outline            TEXT    NULL,
              TowardCertOrExam   TEXT    NULL,
              Note               TEXT    NULL,
              OtherInfo          TEXT    NULL,
              CanRepeat          INTEGER NOT NULL
          );
          CREATE TABLE CourseInCertification (Course_pkid INTEGER NOT NULL, Certification_pkid INTEGER NOT NULL);
          CREATE TABLE CourseJobCategories   (Course_pkid INTEGER NOT NULL, JobCategory_pkid   INTEGER NOT NULL);");

    private static void SeedCourse(IDbConnection conn) => conn.Execute(
        @"INSERT INTO Partner       (pkid, Name)        VALUES (63, '恆逸');
          INSERT INTO CourseGroup   (pkid, Description) VALUES (108, 'IT 必修');
          INSERT INTO PublishStatus (pkid, Description) VALUES (1, '草稿');
          INSERT INTO Course (pkid, Title, OfficialTitle, CourseId, ProdCourseId, FriendlyUrl,
                              DisplayOrder, Partner_pkid, CourseGroup_pkid, PublishStatus_pkid,
                              ScheduleOn, ScheduleOff, Hour, ListPrice, LearningCredit,
                              Material, Objective, Target, Prerequisites, Outline,
                              TowardCertOrExam, Note, OtherInfo, CanRepeat)
          VALUES (62, '網路基礎架構與網路服務', 'Network Infrastructure', 'NINS', 'NINS', 'nins',
                  0, 63, 108, 1, '2015-11-09', '2027-12-31', 42, 16000, 4.0,
                  '講義', @Objective, '<p>對象</p>', '<p>先備</p>', @Outline,
                  '<p>認證</p>', '<p>備註</p>', '<p>其他</p>', 0);",
        new { Outline, Objective });

    private sealed class SqliteDateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override DateOnly Parse(object value) =>
            value switch
            {
                string s => DateOnly.Parse(s),
                DateTime d => DateOnly.FromDateTime(d),
                _ => throw new InvalidCastException($"Cannot convert {value?.GetType().Name} to DateOnly."),
            };

        public override void SetValue(IDbDataParameter parameter, DateOnly value) =>
            parameter.Value = value.ToString("yyyy-MM-dd");
    }

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

    /// <summary>The read paths under test never audit; writes are covered by the repository audit tests.</summary>
    private sealed class NoOpRowAuditWriter : IRowAuditWriter
    {
        public Task LogInsertAsync(IDbConnection conn, IDbTransaction? tx, string tableName, object entity, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task LogUpdateAsync(IDbConnection conn, IDbTransaction? tx, string tableName, object before, object after, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task LogDeleteAsync(IDbConnection conn, IDbTransaction? tx, string tableName, object entity, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
