using EMS.Domain.ReadModels;

namespace EMS.Domain.Interfaces
{
    /// <summary>Reads users as <see cref="PersonReference"/>s, many at once (one query), active or not.</summary>
    public interface IUserReferenceRepository
    {
        /// <summary>The people with these ids (a user that does not exist is absent from the result).</summary>
        /// <summary>Every user (active or not) with only his department: the source for "may this user be chosen / seen" checks, which go through the row-security door.</summary>
        IQueryable<UserDirectoryRow> QueryDirectory();

        Task<Dictionary<long, PersonReference>> GetPeopleAsync(IEnumerable<long?> userIds, CancellationToken cancellationToken = default);
    }
}
