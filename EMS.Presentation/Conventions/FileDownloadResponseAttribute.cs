using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Conventions
{
    /// <summary>
    /// Marks an action that streams a stored file instead of returning the JSON envelope (<c>Task&lt;IActionResult&gt;</c>, a
    /// <c>FileStreamResult</c>). Like <see cref="ExcelResponseAttribute"/> it is a sanctioned exception to the envelope rule; its errors
    /// are still the normal JSON error envelope.
    /// </summary>
    public sealed class FileDownloadResponseAttribute : ProducesResponseTypeAttribute
    {
        public FileDownloadResponseAttribute() : base(typeof(byte[]), StatusCodes.Status200OK, "application/octet-stream") { }
    }
}
