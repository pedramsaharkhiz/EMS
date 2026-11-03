using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.Organization
{
    public class OrganizationDto
    {
        public string Title { get; set; }
        public string Code { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public long OrganizationTypeId { get; set; }
        public long? OrganizationManagementId { get; set; }
        public long? FieldId { get; set; }
        public bool IsActive { get; set; }
    }
}
