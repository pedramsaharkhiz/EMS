using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class TechnicianAssignmentStatusRepository : Repository<TechnicianAssignmentStatus>, ITechnicianAssignmentStatusRepository
    {
        public TechnicianAssignmentStatusRepository(EMSDbContext context) : base(context) { }
    }
}
