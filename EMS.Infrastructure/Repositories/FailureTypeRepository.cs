using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class FailureTypeRepository : Repository<FailureType>, IFailureTypeRepository
    {
        public FailureTypeRepository(EMSDbContext context) : base(context) { }
    }
}
