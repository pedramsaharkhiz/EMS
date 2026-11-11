using EMS.Application.Interfaces.AssignmentNote;
using EMS.Domain.Entities;
using AssignmentEntity = EMS.Domain.Entities.TechnicianAssignment;

namespace EMS.Application.Services.AssignmentNote;

public sealed class ParticipantAssignmentNoteAccessPolicy : IAssignmentNoteAccessPolicy
{
    public bool CanRead(AssignmentEntity assignment, long userId) =>
        assignment.AssignedByUserId == userId || assignment.TechnicianUserId == userId;
    public bool CanCreate(AssignmentEntity assignment, long userId) => assignment.AssignedByUserId == userId;
    public bool CanEdit(EMS.Domain.Entities.AssignmentNote note, long userId) =>
        note.CreatedByUserId == userId && CanRead(note.TechnicianAssignment, userId);
    public bool CanMarkSeen(AssignmentEntity assignment, long userId) => assignment.TechnicianUserId == userId;
}
