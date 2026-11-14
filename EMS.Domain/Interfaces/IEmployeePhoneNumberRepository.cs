using EMS.Domain.Entities;

namespace EMS.Domain.Interfaces;

public interface IEmployeePhoneNumberRepository : IRepository<EmployeePhoneNumber>
{
    IQueryable<EmployeePhoneNumber> QueryAll(bool tracked = false);
}
