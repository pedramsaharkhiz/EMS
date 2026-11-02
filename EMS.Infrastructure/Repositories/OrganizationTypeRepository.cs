using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class OrganizationTypeRepository : Repository<OrganizationType>, IOrganizationTypeRepository
    {
        public OrganizationTypeRepository(EMSDbContext context) : base(context) { }
    }
}
