using EMS.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace EMS.Presentation.Extensions
{
    /// <summary>
    /// Documents the response envelope of every action (OpenAPI / the API contract test) in one place instead of on each action:
    /// the success response is the <see cref="ApiResponse{T}"/> the action returns (unless the action declares its own 2xx, e.g. 201
    /// for create), and the errors every action can produce (400, 500, plus 401 and 403 for protected actions) are
    /// <c>ApiResponse&lt;object&gt;</c>.
    /// </summary>
    public sealed class ApiResponseConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            var declared = action.Filters.OfType<IApiResponseMetadataProvider>().Select(p => p.StatusCode).ToHashSet();

            if (!declared.Any(code => code is >= 200 and < 300) && SuccessTypeOf(action.ActionMethod.ReturnType) is { } success)
            {
                action.Filters.Add(new ProducesResponseTypeAttribute(success, StatusCodes.Status200OK));
            }

            var anonymous = action.Attributes.OfType<IAllowAnonymous>().Any();
            var protectedAction = !anonymous &&
                (action.Attributes.OfType<IAuthorizeData>().Any() || action.Controller.Attributes.OfType<IAuthorizeData>().Any());

            var errors = new List<int> { StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError };
            if (!anonymous) errors.Add(StatusCodes.Status401Unauthorized);
            if (protectedAction) errors.Add(StatusCodes.Status403Forbidden);

            foreach (var code in errors.Where(code => !declared.Contains(code)))
                action.Filters.Add(new ProducesResponseTypeAttribute(typeof(ApiResponse<object>), code));
        }

        /// <summary>The <c>ApiResponse&lt;T&gt;</c> in <c>Task&lt;ActionResult&lt;ApiResponse&lt;T&gt;&gt;&gt;</c>, or null for any other return type.</summary>
        public static Type? SuccessTypeOf(Type returnType)
        {
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                returnType = returnType.GetGenericArguments()[0];

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ActionResult<>))
            {
                var inner = returnType.GetGenericArguments()[0];
                if (inner.IsGenericType && inner.GetGenericTypeDefinition() == typeof(ApiResponse<>))
                    return inner;
            }

            return null;
        }
    }
}
