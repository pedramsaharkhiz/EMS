using EMS.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Extensions
{
    public static class QueryableExtensions 
    { 
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default) 
        { 
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return new PagedResult<T> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = totalCount };
        }
    }
}
