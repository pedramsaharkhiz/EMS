using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class ReportTypeRepository : Repository<ReportType>, IReportTypeRepository
    {
        public ReportTypeRepository(EMSDbContext context) : base(context) { }
    }
}
