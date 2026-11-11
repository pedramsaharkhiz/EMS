using EMS.Application.Common.People;
using Xunit;

namespace EMS.Tests.Unit
{
    /// <summary>Every DTO that shows a person must show the same full set of fields for him (name, personnel code, state, post, place of work).</summary>
    public class PersonReferenceConsistencyTests
    {
        [Theory]
        [InlineData(typeof(EMS.Application.Dtos.AssignmentNote.AssignmentNoteItemDto), "CreatedBy")]
        [InlineData(typeof(EMS.Application.Dtos.Equipment.EquipmentListItemDto), "Technician")]
        [InlineData(typeof(EMS.Application.Dtos.EquipmentLocationHistory.EquipmentLocationHistoryItemDto), "Technician")]
        [InlineData(typeof(EMS.Application.Dtos.EquipmentLocationHistory.EquipmentLocationHistoryItemDto), "ChangedBy")]
        [InlineData(typeof(EMS.Application.Dtos.FailureReport.FailureReportListItemDto), "ReportedBy")]
        [InlineData(typeof(EMS.Application.Dtos.Attachment.AttachmentItemDto), "CreatedBy")]
        [InlineData(typeof(EMS.Application.Dtos.TechnicianAssignment.TechnicianAssignmentListItemDto), "Technician")]
        [InlineData(typeof(EMS.Application.Dtos.TechnicianAssignment.TechnicianAssignmentListItemDto), "AssignedBy")]
        public void A_dto_with_a_person_carries_the_full_person_set(Type dto, string prefix)
        {
            var missing = PersonReferenceMapper.PropertyNames(prefix)
                .Where(n => dto.GetProperty(n) is null).ToList();
            Assert.True(missing.Count == 0, $"{dto.Name} lacks: {string.Join(", ", missing)}");
        }
    }
}
