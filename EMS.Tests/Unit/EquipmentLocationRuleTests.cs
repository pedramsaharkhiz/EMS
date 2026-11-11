using EMS.Application.Common.Exceptions;
using EMS.Application.Services.Equipment;

namespace EMS.Tests.Unit;

public class EquipmentLocationRuleTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    public async Task A_technician_is_optional_only_at_a_department(bool branch, bool technician, bool allowed)
    {
        var location = new EquipmentLocation(branch ? 1 : null, branch ? null : 2, technician ? 3 : null);
        var rule = new DepartmentTechnicianLocationRule();
        if (allowed)
            await rule.ValidateAsync(location, default);
        else
        {
            var error = await Assert.ThrowsAsync<BadRequestException>(() => rule.ValidateAsync(location, default));
            Assert.Equal(EquipmentLocation.TechnicianMessage, error.Message);
        }
    }
}
