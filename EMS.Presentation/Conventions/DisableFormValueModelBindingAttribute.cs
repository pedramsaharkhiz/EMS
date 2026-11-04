using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EMS.Presentation.Conventions
{
    /// <summary>
    /// Stops MVC from reading the request body as a form before the action runs. Without it the form value provider buffers the whole
    /// multipart body (in memory, then on the app server's disk), which is exactly what a streamed upload must avoid; the action then
    /// reads the body itself with a <c>MultipartReader</c>. Use it only on actions that stream their body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var factories = context.ValueProviderFactories;
            factories.RemoveType<FormValueProviderFactory>();
            factories.RemoveType<FormFileValueProviderFactory>();
            factories.RemoveType<JQueryFormValueProviderFactory>();
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }
}
