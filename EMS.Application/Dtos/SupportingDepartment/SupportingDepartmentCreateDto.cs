using System;
using System.Collections.Generic;
using System.Text;

using EMS.Application.Common.Catalog;
namespace EMS.Application.Dtos.SupportingDepartment
{
    public class SupportingDepartmentCreateDto : ICatalogCreateRequest
    {
        public long CityId { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}