namespace EMS.Application.Dtos.Equipment
{
    /// <summary>
    /// The technician of one equipment, with his post and supporting department. <see cref="IsActive"/> is false when the user was
    /// deactivated after being assigned (the equipment keeps its technician). No phone number or e-mail is shown on purpose.
    /// </summary>
    public class EquipmentTechnicianDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PersonnelCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public long? PostId { get; set; }
        public string? PostTitle { get; set; }
        public long? SupportingDepartmentId { get; set; }
        public string? SupportingDepartmentTitle { get; set; }
    }
}
