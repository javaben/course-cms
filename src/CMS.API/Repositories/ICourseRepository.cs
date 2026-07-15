using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ICourseRepository
{
    Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Course>> QueryAsync(CourseQuery query, CancellationToken ct = default);

    /// <summary>Single course by pkid — includes the two N-N id lists. Null if not found.</summary>
    Task<Course?> GetByIdAsync(int pkid, CancellationToken ct = default);

    /// <summary>Insert (IDENTITY pkid) + sync both junctions in one transaction; returns the created row.</summary>
    Task<Course> CreateAsync(CourseRequest request, CancellationToken ct = default);

    /// <summary>Update scalar fields + re-sync both junctions. False if the pkid does not exist.</summary>
    Task<bool> UpdateAsync(CourseRequest request, CancellationToken ct = default);

    Task<bool> DeleteAsync(int pkid, CancellationToken ct = default);
}
