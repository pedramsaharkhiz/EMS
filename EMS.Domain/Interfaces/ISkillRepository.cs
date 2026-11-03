using EMS.Domain.Entities;

namespace EMS.Domain.Interfaces;

public interface ISkillRepository : ICatalogRepository<Skill>
{
    IQueryable<Skill> QueryForRead();
}
