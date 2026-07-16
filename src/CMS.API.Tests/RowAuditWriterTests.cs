using System.Security.Claims;
using CMS.API.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace CMS.API.Tests;

/// <summary>
/// Unit tests for <see cref="RowAuditWriter"/>'s reflection logic — the row-building rules only, no
/// database. The <c>Build*</c> methods never touch the connection factory, so a null factory is fine.
/// </summary>
public class RowAuditWriterTests
{
    // Declaration order matters: pkid first, then Title is the FIRST string property.
    private sealed class SampleEntity
    {
        public int Pkid { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
    }

    private static RowAuditWriter WriterFor(ClaimsPrincipal? user)
    {
        var accessor = new HttpContextAccessor();
        if (user is not null)
            accessor.HttpContext = new DefaultHttpContext { User = user };
        return new RowAuditWriter(accessor);
    }

    private static ClaimsPrincipal UserNamed(string userName)
        => new(new ClaimsIdentity(new[] { new Claim("userName", userName) }, authenticationType: "test"));

    // --- ActionDesc: first string property (Insert / Delete) --------------------------------------

    [Fact]
    public void LogInsert_uses_first_string_property_as_ActionDesc_and_reads_pkid()
    {
        var writer = WriterFor(UserNamed("alice"));
        var entity = new SampleEntity { Pkid = 42, Title = "Intro to C#", Description = "later string" };

        var row = writer.BuildInsertAudit("Course", entity);

        Assert.Equal("Course", row.TableName);
        Assert.Equal("Insert", row.ActionType);
        Assert.Equal("Intro to C#", row.ActionDesc);   // Title, not Description
        Assert.Equal("42", row.PrimaryKeyValues);
        Assert.Equal("alice", row.UserName);
    }

    [Fact]
    public void LogDelete_uses_first_string_property_as_ActionDesc()
    {
        var writer = WriterFor(UserNamed("bob"));
        var entity = new SampleEntity { Pkid = 7, Title = "Deleted course" };

        var row = writer.BuildDeleteAudit("Course", entity);

        Assert.Equal("Delete", row.ActionType);
        Assert.Equal("Deleted course", row.ActionDesc);
        Assert.Equal("7", row.PrimaryKeyValues);
    }

    // --- ActionDesc: changed property names (Update) ----------------------------------------------

    [Fact]
    public void LogUpdate_lists_exactly_the_changed_property_names_in_order()
    {
        var writer = WriterFor(UserNamed("alice"));
        var before = new SampleEntity { Pkid = 1, Title = "Old", Description = "same", DisplayOrder = 3 };
        var after = new SampleEntity { Pkid = 1, Title = "New", Description = "same", DisplayOrder = 5 };

        var row = writer.BuildUpdateAudit("Course", before, after);

        Assert.Equal("Update", row.ActionType);
        Assert.Equal("Title, DisplayOrder", row.ActionDesc);  // exactly the changed ones, in order
        Assert.Equal("1", row.PrimaryKeyValues);              // pkid read from `after`
    }

    [Fact]
    public void LogUpdate_ActionDesc_is_empty_when_nothing_changed()
    {
        var writer = WriterFor(UserNamed("alice"));
        var entity = new SampleEntity { Pkid = 1, Title = "Same", Description = "same", DisplayOrder = 2 };
        var copy = new SampleEntity { Pkid = 1, Title = "Same", Description = "same", DisplayOrder = 2 };

        var row = writer.BuildUpdateAudit("Course", entity, copy);

        Assert.Equal("", row.ActionDesc);
    }

    [Fact]
    public void BuildUpdateAudit_ignores_non_scalar_navigation_and_collection_properties()
    {
        var writer = WriterFor(UserNamed("alice"));
        // Only the scalar Title differs; the nav object and the list differ by reference but are not
        // table columns, so they must not appear in the changed-column list.
        var before = new RichEntity { Pkid = 1, Title = "Old", Nav = new(), Tags = new() { 1 } };
        var after = new RichEntity { Pkid = 1, Title = "New", Nav = new(), Tags = new() { 2 } };

        var row = writer.BuildUpdateAudit("Course", before, after);

        Assert.Equal("Title", row.ActionDesc);
    }

    private sealed class NavObject { public int X { get; set; } }

    private sealed class RichEntity
    {
        public int Pkid { get; set; }
        public string Title { get; set; } = "";
        public NavObject? Nav { get; set; }
        public List<int> Tags { get; set; } = new();
    }

    // --- UserName resolution ----------------------------------------------------------------------

    [Fact]
    public void UserName_falls_back_to_system_with_no_authenticated_user()
    {
        var writer = WriterFor(user: null);   // no HttpContext at all
        var row = writer.BuildInsertAudit("Course", new SampleEntity { Pkid = 1, Title = "x" });

        Assert.Equal("system", row.UserName);
    }

    [Fact]
    public void UserName_falls_back_to_system_when_user_has_no_name_claim()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());  // not authenticated, no claims
        var writer = WriterFor(anonymous);

        var row = writer.BuildInsertAudit("Course", new SampleEntity { Pkid = 1, Title = "x" });

        Assert.Equal("system", row.UserName);
    }

    // --- Truncation -------------------------------------------------------------------------------

    [Fact]
    public void ActionDesc_truncates_at_1000_characters()
    {
        var writer = WriterFor(UserNamed("alice"));
        var entity = new SampleEntity { Pkid = 1, Title = new string('x', 1500) };

        var row = writer.BuildInsertAudit("Course", entity);

        Assert.Equal(RowAuditWriter.MaxActionDescLength, row.ActionDesc!.Length);
        Assert.Equal(1000, row.ActionDesc.Length);
    }
}
