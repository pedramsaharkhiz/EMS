using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.Brand
{
    public class BrandUpdateDto
    {
        public long BrandId { get; set; }
        public string? Title { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
