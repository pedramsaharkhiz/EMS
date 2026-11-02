using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.Brand
{
    public class BrandDetailDto
    {
        public long BrandId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
