using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.RefreshToken
{
    public class RegisterUserRequestDto
    {
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? PersonnelCode { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
