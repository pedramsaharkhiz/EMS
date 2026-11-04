using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class PostRepository : CatalogRepository<Post>, IPostRepository
    {
        public PostRepository(EMSDbContext context) : base(context) { }

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default) =>
            await _context.Users.AnyAsync(x => x.PostId == id && x.IsActive, cancellationToken);
    }
}
