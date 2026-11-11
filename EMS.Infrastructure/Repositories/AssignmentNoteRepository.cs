using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories;

public sealed class AssignmentNoteRepository : Repository<AssignmentNote>, IAssignmentNoteRepository
{
    public AssignmentNoteRepository(EMSDbContext context) : base(context) { }

    // Update only the note; the included assignment/category must retain their own versions and audit state.
    public override void Update(AssignmentNote entity) => _context.Entry(entity).State = EntityState.Modified;

    public IQueryable<AssignmentNote> QueryOfAssignment(long assignmentId, bool active) =>
        _dbSet.AsNoTracking().IncludingInactive().Include(x => x.NoteCategory)
            .Where(x => x.TechnicianAssignmentId == assignmentId && x.IsActive == active);

    public Task<AssignmentNote?> GetAsync(long id, bool tracked, CancellationToken cancellationToken) =>
        (tracked ? _dbSet.AsQueryable() : _dbSet.AsNoTracking()).IncludingInactive()
            .Include(x => x.TechnicianAssignment).Include(x => x.NoteCategory)
            .FirstOrDefaultAsync(x => x.AssignmentNoteId == id, cancellationToken);
}
