using EMS.Domain.Entities;

namespace EMS.Domain.Interfaces;

public interface IAssignmentNoteRepository : IRepository<AssignmentNote>
{
    IQueryable<AssignmentNote> QueryOfAssignment(long assignmentId, bool active);
    Task<AssignmentNote?> GetAsync(long id, bool tracked, CancellationToken cancellationToken);
}
