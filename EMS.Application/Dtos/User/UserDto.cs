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
        public string PersonnelCode { get; set; }
        public string NationalCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public List<long>? SupportingDepartmentIds { get; set; }
        public bool IsActive { get; set; }
    }
}