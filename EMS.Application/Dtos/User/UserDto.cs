using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.User
{
    public class UserDto
    {
        public string UserName { get; set; }
        public string NormalizedUserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        private string _personnelCode = string.Empty;
        public string PersonnelCode { get => _personnelCode; set => _personnelCode = EMS.Domain.UserIdentityRules.Normalize(value); }
        private string? _nationalCode;
        public string? NationalCode { get => _nationalCode; set => _nationalCode = EMS.Domain.UserIdentityRules.NormalizeOptional(value); }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public List<long>? SupportingDepartmentIds { get; set; }
        public bool IsActive { get; set; }
    }
}