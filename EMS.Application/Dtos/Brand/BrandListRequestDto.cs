using EMS.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.Brand
{
    public class BrandListRequestDto : PagedRequest
    {
        public string? Title { get; set; }
        public string? Code { get; set; }
        //public bool IsActive { get; set; } = true;
    }
}
