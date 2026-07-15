using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ICourseGroupRepository
{
    Task<IReadOnlyList<CourseGroup>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CourseGroup>> QueryAsync(CourseGroupQuery query, CancellationToken ct = default);
    Task<CourseGroup?> GetByIdAsync(short pkid, CancellationToken ct = default);

    /// <summary>Inserts the group (pkid auto-generated). Returns the created record.</summary>
    Task<CourseGroup> CreateAsync(CourseGroupRequest request, CancellationToken ct = default);

    /// <summary>Updates the group (pkid immutable). Returns false if it does not exist.</summary>
    Task<bool> UpdateAsync(CourseGroupRequest request, CancellationToken ct = default);

    Task<bool> DeleteAsync(short pkid, CancellationToken ct = default);
}
