using EMS.Application.Common.Export;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Conventions
{
    /// <summary>
    /// Marks an action that returns an Excel file instead of the JSON envelope (<c>Task&lt;IActionResult&gt;</c>, built with
    /// <c>ExcelDownload</c> of <see cref="Controllers.BaseApiController"/>). It is the one sanctioned exception to the envelope rule;
    /// errors of such an action are still the normal JSON error envelope.
    /// </summary>
    public sealed class ExcelResponseAttribute : ProducesResponseTypeAttribute
    {
        public ExcelResponseAttribute() : base(typeof(byte[]), StatusCodes.Status200OK, ExcelFile.ContentType) { }
    }
}
