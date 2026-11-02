namespace EMS.Application.Interfaces.Auth
{
    /// <summary>
    /// Invalidates the access tokens of users whose effective access changed
    /// (their security stamp is rotated, see <see cref="IAccessTokenValidator"/>).
    /// </summary>
    public interface ISecurityStampInvalidator
    {
        /// <summary>Invalidates the tokens of every user that currently holds the role.</summary>
        Task InvalidateRoleUsersAsync(long roleId, CancellationToken cancellationToken = default);
    }
}
