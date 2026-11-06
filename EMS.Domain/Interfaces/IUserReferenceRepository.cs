using EMS.Domain.ReadModels;

namespace EMS.Domain.Interfaces
{
    /// <summary>Reads users as <see cref="PersonReference"/>s, many at once (one query), active or not.</summary>
    public interface IUserReferenceRepository
    {
        /// <summary>The people with these ids (a user that does not exist is absent from the result).</summary>
        Task<Dictionary<long, PersonReference>> GetPeopleAsync(IEnumerable<long?> userIds, CancellationToken cancellationToken = default);
    }
}
