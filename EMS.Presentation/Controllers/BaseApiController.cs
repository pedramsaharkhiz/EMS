using EMS.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    /// <summary>
    /// Base of every API controller. Actions never return a bare DTO: they return one of the helpers below, so every
    /// response is an <see cref="ApiResponse{T}"/> (see docs/API-CONVENTIONS.md). A test fails if an action does not.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        public const string CreatedMessage = "با موفقیت ایجاد شد.";
        public const string UpdatedMessage = "با موفقیت ویرایش شد.";
        public const string DeletedMessage = "با موفقیت حذف شد.";
        public const string DoneMessage = "عملیات با موفقیت انجام شد.";

        private string TraceId => HttpContext.TraceIdentifier;

        /// <summary>200 with the resource in <c>data</c> (reads).</summary>
        protected ActionResult<ApiResponse<T>> Success<T>(T data, string? message = null) =>
            Ok(ApiResponse.Ok(data, TraceId, message));

        /// <summary>200 with the page's items in <c>data</c> and the paging information in <c>meta.pagination</c>.</summary>
        protected ActionResult<ApiResponse<IReadOnlyList<T>>> Paged<T>(PagedResult<T> page) =>
            Ok(ApiResponse.Page(page, TraceId));

        /// <summary>201 with a Location header pointing at the new resource.</summary>
        protected ActionResult<ApiResponse<T>> CreatedAt<T>(string actionName, object routeValues, T data, string? message = null) =>
            CreatedAtAction(actionName, routeValues, ApiResponse.Ok(data, TraceId, message ?? CreatedMessage));

        /// <summary>200 with the updated resource in <c>data</c>.</summary>
        protected ActionResult<ApiResponse<T>> Updated<T>(T data, string? message = null) =>
            Ok(ApiResponse.Ok(data, TraceId, message ?? UpdatedMessage));

        /// <summary>200 with <c>data = null</c> (delete).</summary>
        protected ActionResult<ApiResponse<object>> Deleted(string? message = null) =>
            Ok(ApiResponse.Empty(TraceId, message ?? DeletedMessage));

        /// <summary>200 with <c>data = null</c> (a command that has nothing to return: assign roles, set a password, ...).</summary>
        protected ActionResult<ApiResponse<object>> Done(string? message = null) =>
            Ok(ApiResponse.Empty(TraceId, message ?? DoneMessage));
    }
}
