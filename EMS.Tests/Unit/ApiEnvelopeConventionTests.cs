using EMS.Application.Common.Models;
using EMS.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace EMS.Tests.Unit
{
    /// <summary>
    /// The response envelope is enforced for new code too: an action that returns a bare DTO, <c>IActionResult</c> or
    /// <c>NoContent()</c> would give the front end a second response shape, so it fails here instead of in production.
    /// </summary>
    public class ApiEnvelopeConventionTests
    {
        private static IEnumerable<MethodInfo> Actions() =>
            typeof(BaseApiController).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(BaseApiController).IsAssignableFrom(t))
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(m => m.GetCustomAttributes().OfType<HttpMethodAttribute>().Any());

        [Fact]
        public void There_are_actions_to_check()
        {
            Assert.True(Actions().Count() > 40);
        }

        [Fact]
        public void Every_action_returns_the_api_response_envelope()
        {
            var offenders = Actions()
                .Where(m => m.GetCustomAttribute<EMS.Presentation.Conventions.ExcelResponseAttribute>() is null)   // the one sanctioned exception: Excel downloads
                .Where(m => EMS.Presentation.Conventions.ApiResponseConvention.SuccessTypeOf(m.ReturnType) is null)
                .Select(m => $"{m.DeclaringType!.Name}.{m.Name} returns {m.ReturnType.Name}")
                .ToList();

            Assert.True(offenders.Count == 0,
                "Actions must return Task<ActionResult<ApiResponse<T>>> (use Success/Paged/CreatedAt/Updated/Deleted/Done of BaseApiController):\n  " + string.Join("\n  ", offenders));
        }

        [Fact]
        public void An_excel_action_returns_IActionResult_and_is_a_GET()
        {
            var excel = Actions().Where(m => m.GetCustomAttribute<EMS.Presentation.Conventions.ExcelResponseAttribute>() is not null).ToList();
            foreach (var action in excel)
            {
                Assert.Equal(typeof(Task<IActionResult>), action.ReturnType);
                Assert.All(action.GetCustomAttributes().OfType<HttpMethodAttribute>(), a => Assert.Equal(new[] { "GET" }, a.HttpMethods));
            }
        }
    }
}
