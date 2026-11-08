using EMS.Domain;
using EMS.Domain.Enums;

namespace EMS.Tests.Unit
{
    public class TechnicianAssignmentCatalogTests
    {
        private static readonly TechnicianAssignmentStatusCode[] All = Enum.GetValues<TechnicianAssignmentStatusCode>().Where(c => c != TechnicianAssignmentStatusCode.Unknown).ToArray();

        [Fact]
        public void Every_status_has_a_title_and_an_id_equal_to_its_value()
        {
            Assert.Equal(All.Length, TechnicianAssignmentCatalog.Titles.Count);
            Assert.All(All, c =>
            {
                Assert.Equal((long)c, TechnicianAssignmentCatalog.IdOf(c));
                Assert.False(string.IsNullOrWhiteSpace(TechnicianAssignmentCatalog.TitleOf(c)));
            });
        }

        [Fact]
        public void Only_the_documented_transitions_are_allowed()
        {
            var allowed = new HashSet<(TechnicianAssignmentStatusCode, TechnicianAssignmentStatusCode)>
            {
                (TechnicianAssignmentStatusCode.Assigned, TechnicianAssignmentStatusCode.Accepted),
                (TechnicianAssignmentStatusCode.Assigned, TechnicianAssignmentStatusCode.Rejected),
                (TechnicianAssignmentStatusCode.Assigned, TechnicianAssignmentStatusCode.Cancelled),
                (TechnicianAssignmentStatusCode.Accepted, TechnicianAssignmentStatusCode.InProgress),
                (TechnicianAssignmentStatusCode.Accepted, TechnicianAssignmentStatusCode.Cancelled),
                (TechnicianAssignmentStatusCode.InProgress, TechnicianAssignmentStatusCode.Completed),
                (TechnicianAssignmentStatusCode.InProgress, TechnicianAssignmentStatusCode.Cancelled),
            };

            foreach (var from in All)
                foreach (var to in All)
                    Assert.Equal(allowed.Contains((from, to)), TechnicianAssignmentCatalog.CanMove(from, to));
        }

        [Fact]
        public void Final_statuses_have_no_way_out_and_the_open_ones_are_exactly_those_with_one()
        {
            var final = new[] { TechnicianAssignmentStatusCode.Rejected, TechnicianAssignmentStatusCode.Completed, TechnicianAssignmentStatusCode.Cancelled };
            foreach (var status in final)
            {
                Assert.False(TechnicianAssignmentCatalog.IsOpen((long)status));
                Assert.All(All, to => Assert.False(TechnicianAssignmentCatalog.CanMove(status, to)));
            }
            foreach (var status in All.Except(final))
            {
                Assert.True(TechnicianAssignmentCatalog.IsOpen((long)status));
                Assert.Contains(All, to => TechnicianAssignmentCatalog.CanMove(status, to));
            }
        }
    }
}
