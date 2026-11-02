using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Common.Models
{
    public abstract class PagedRequest 
    { 
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        //public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string SortDirection { get; set; } = "asc";
    }
}
