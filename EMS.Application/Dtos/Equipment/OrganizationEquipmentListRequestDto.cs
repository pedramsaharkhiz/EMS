using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.Equipment
{
    /// <summary>Requests a paged list of active equipment currently located at an organization.</summary>
    public sealed class OrganizationEquipmentListRequestDto : PagedRequest
    {
        /// <summary>The organization whose equipment will be listed.</summary>
        public long OrganizationId { get; set; }

        /// <summary>Searches the Yaas code, serial number and asset number.</summary>
        public string? Search { get; set; }
    }
}
