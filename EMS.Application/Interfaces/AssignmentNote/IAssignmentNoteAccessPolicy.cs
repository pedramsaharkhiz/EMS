using EMS.Domain.Entities;
using AssignmentEntity = EMS.Domain.Entities.TechnicianAssignment;

namespace EMS.Application.Interfaces.AssignmentNote;

// Keep all participant rules together: future access rules replace this policy, not individual endpoints.
public interface IAssignmentNoteAccessPolicy
{
    bool CanRead(AssignmentEntity assignment, long userId);
    bool CanCreate(AssignmentEntity assignment, long userId);
    bool CanEdit(EMS.Domain.Entities.AssignmentNote note, long userId);
    bool CanMarkSeen(AssignmentEntity assignment, long userId);
}
