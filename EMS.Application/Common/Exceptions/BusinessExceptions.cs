namespace EMS.Application.Common.Exceptions
{
    /// <summary>
    /// Stable, machine-readable error codes returned to API clients in the <c>code</c> field of every error response.
    /// Clients should branch on the code, never on the (Persian, human-readable) message.
    /// Codes are part of the API contract: add new ones freely, never rename or reuse existing ones.
    /// </summary>
    public static class ErrorCodes
    {
        // generic, one per HTTP status
        public const string BadRequest = "bad_request";
        public const string ValidationFailed = "validation_failed";
        public const string Unauthorized = "unauthorized";
        public const string Forbidden = "forbidden";
        public const string NotFound = "not_found";
        public const string MethodNotAllowed = "method_not_allowed";
        public const string UnsupportedMediaType = "unsupported_media_type";
        public const string NotAcceptable = "not_acceptable";
        public const string Conflict = "conflict";
        public const string InternalError = "internal_error";

        // authentication
        public const string InvalidCredentials = "invalid_credentials";
        public const string InvalidRefreshToken = "invalid_refresh_token";
        public const string InvalidUser = "invalid_user";

        // data integrity
        public const string ConcurrencyConflict = "concurrency_conflict";
        public const string UniqueViolation = "unique_violation";
        public const string ForeignKeyViolation = "foreign_key_violation";
        public const string InvalidRowVersion = "invalid_row_version";

        // catalogs
        public const string DuplicateTitle = "duplicate_title";
        public const string HasActiveDependents = "has_active_dependents";
        public const string ParentNotFound = "parent_not_found";
        public const string ParentInactive = "parent_inactive";
        public const string ProtectedRecord = "protected_record";
        public const string ReservedCode = "reserved_code";

        // exports
        public const string ExportTooLarge = "export_too_large";
        public const string ExportBusy = "export_busy";
        public const string TooManyRequests = "too_many_requests";

        // users and roles
        public const string UserNameTaken = "user_name_taken";
        public const string PersonnelCodeTaken = "personnel_code_taken";
        public const string NationalCodeTaken = "national_code_taken";
        public const string UserNotFound = "user_not_found";
        public const string UserInactive = "user_inactive";
        public const string UsersInvalid = "users_invalid";
        public const string DepartmentNotFound = "department_not_found";
        public const string RoleNameRequired = "role_name_required";
        public const string RoleNameTaken = "role_name_taken";
        public const string RoleNotFound = "role_not_found";
        public const string RoleInactive = "role_inactive";
        public const string RolesInvalid = "roles_invalid";
        public const string UserAlreadyHasRole = "user_already_has_role";
        public const string SkillsInvalid = "skills_invalid";
        public const string RoleInUse = "role_in_use";
        public const string IdentityError = "identity_error";
    }

    /// <summary>A business-rule failure with a stable error code.</summary>
    public abstract class AppException : Exception
    {
        public string Code { get; }

        protected AppException(string message, string code) : base(message)
        {
            Code = code;
        }
    }

    public sealed class NotFoundException : AppException
    {
        public NotFoundException(string message, string? code = null) : base(message, code ?? ErrorCodes.NotFound) { }
    }

    public sealed class ConflictException : AppException
    {
        public ConflictException(string message, string? code = null) : base(message, code ?? ErrorCodes.Conflict) { }
    }

    /// <summary>A 429: the caller must wait and try again (e.g. too many exports running at once).</summary>
    public sealed class TooManyRequestsException : AppException
    {
        public TooManyRequestsException(string message, string? code = null) : base(message, code ?? ErrorCodes.TooManyRequests) { }
    }

    public sealed class BadRequestException : AppException
    {
        public BadRequestException(string message, string? code = null) : base(message, code ?? ErrorCodes.BadRequest) { }
    }

    /// <summary>A 401 with a stable error code (still an <see cref="UnauthorizedAccessException"/> for existing handlers).</summary>
    public sealed class UnauthorizedAppException : UnauthorizedAccessException
    {
        public string Code { get; }

        public UnauthorizedAppException(string message, string? code = null) : base(message)
        {
            Code = code ?? ErrorCodes.Unauthorized;
        }
    }
}
