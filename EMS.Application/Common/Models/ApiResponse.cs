namespace EMS.Application.Common.Models
{
    /// <summary>
    /// The one response shape of the whole API, for successes and errors alike. Every key is always present
    /// (<c>null</c> when it does not apply), so a client declares a single type and never checks for missing keys.
    /// <list type="bullet">
    /// <item><c>success</c>: true only for 2xx responses (the HTTP status code stays meaningful too).</item>
    /// <item><c>message</c>: text that may be shown to the user (always set for errors and for create/update/delete).</item>
    /// <item><c>data</c>: the resource; an array for lists; null for commands and errors.</item>
    /// <item><c>meta</c>: extra information about <c>data</c>, today only <c>pagination</c> of paged lists.</item>
    /// <item><c>error</c>: set only when <c>success</c> is false: a stable machine-readable <c>code</c> plus field-level <c>details</c>.</item>
    /// <item><c>traceId</c>: the correlation id of the request (also the X-Correlation-ID response header); quote it to support.</item>
    /// </list>
    /// See docs/API-CONVENTIONS.md.
    /// </summary>
    public sealed class ApiResponse<T>
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public T? Data { get; init; }
        public ApiMeta? Meta { get; init; }
        public ApiError? Error { get; init; }
        public string TraceId { get; init; } = string.Empty;
    }

    public sealed class ApiMeta
    {
        public ApiPagination? Pagination { get; init; }
    }

    public sealed class ApiPagination
    {
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
        public bool HasPreviousPage { get; init; }
        public bool HasNextPage { get; init; }
    }

    public sealed class ApiError
    {
        /// <summary>Stable machine-readable code (see <see cref="Exceptions.ErrorCodes"/>). Clients branch on this, never on the message.</summary>
        public string Code { get; init; } = string.Empty;

        /// <summary>Field-level problems (validation); empty for errors that are not about a field.</summary>
        public IReadOnlyList<ApiErrorDetail> Details { get; init; } = [];
    }

    public sealed class ApiErrorDetail
    {
        /// <summary>camelCase name of the offending request field (same spelling as in the JSON body); nested fields are dotted.</summary>
        public string Field { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    /// <summary>Factories for <see cref="ApiResponse{T}"/>.</summary>
    public static class ApiResponse
    {
        public static ApiResponse<T> Ok<T>(T data, string traceId, string? message = null) =>
            new() { Success = true, Message = message, Data = data, TraceId = traceId };

        public static ApiResponse<IReadOnlyList<T>> Page<T>(PagedResult<T> page, string traceId) =>
            new()
            {
                Success = true,
                Data = page.Items,
                Meta = new ApiMeta
                {
                    Pagination = new ApiPagination
                    {
                        PageNumber = page.PageNumber,
                        PageSize = page.PageSize,
                        TotalCount = page.TotalCount,
                        TotalPages = page.TotalPages,
                        HasPreviousPage = page.HasPreviousPage,
                        HasNextPage = page.HasNextPage
                    }
                },
                TraceId = traceId
            };

        /// <summary>A successful command with nothing to return (delete, assign, ...).</summary>
        public static ApiResponse<object> Empty(string traceId, string? message = null) =>
            new() { Success = true, Message = message, TraceId = traceId };

        public static ApiResponse<object> Fail(string traceId, string code, string message, IEnumerable<ApiErrorDetail>? details = null) =>
            new()
            {
                Success = false,
                Message = message,
                Error = new ApiError { Code = code, Details = details?.ToList() ?? [] },
                TraceId = traceId
            };
    }
}
