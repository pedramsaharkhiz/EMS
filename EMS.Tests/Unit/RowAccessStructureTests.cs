using System.Text.RegularExpressions;

namespace EMS.Tests.Unit
{
    /// <summary>
    /// Guards that protected rows are only ever reached through the central door. The services get their rows from ONE method each (VisibleRows / VisibleUsers) that
    /// goes through IRowAccess; these tests read the source and refuse a new read that bypasses it. (Reading a record by its id and the writes are not covered yet:
    /// they are phase 2, see docs/RLS-FINDINGS.md.)
    /// </summary>
    public class RowAccessStructureTests
    {
        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EMS.slnx")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("EMS.slnx was not found above " + AppContext.BaseDirectory);
        }

        private static IEnumerable<(string File, int Line, string Text)> Lines(string relativeFolder, string? excludeFolder = null)
        {
            var folder = Path.Combine(RepositoryRoot(), relativeFolder);
            foreach (var file in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
            {
                if (excludeFolder is not null && file.Contains(Path.DirectorySeparatorChar + excludeFolder + Path.DirectorySeparatorChar))
                    continue;
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                    yield return (Path.GetRelativePath(RepositoryRoot(), file), i + 1, lines[i]);
            }
        }

        [Fact]
        public void A_service_reads_the_rows_of_a_protected_type_only_through_the_row_door()
        {
            // QueryRows is the repository's raw source of rows. Outside the attachment module (attachments are protected through the record they belong to, phase 2),
            // every call must sit inside the line that hands the rows to IRowAccess.Apply.
            var offenders = Lines("EMS.Application/Services", excludeFolder: "Attachment")
                .Where(l => Regex.IsMatch(l.Text, @"\.QueryRows\(") && !l.Text.TrimStart().StartsWith("//") && !l.Text.TrimStart().StartsWith("///"))
                .Where(l => !l.Text.Contains("_access.Apply("))
                .Select(l => $"{l.File}:{l.Line}: {l.Text.Trim()}")
                .ToList();

            Assert.True(offenders.Count == 0, "These reads bypass IRowAccess.Apply:\n" + string.Join("\n", offenders));
        }

        [Fact]
        public void Each_visible_rows_method_hands_its_rows_to_the_row_door()
        {
            var sources = new (string File, string Marker)[]
            {
                ("EMS.Application/Services/Equipment/EquipmentService.cs", "VisibleRows(bool active)"),
                ("EMS.Application/Services/FailureReport/FailureReportService.cs", "VisibleRows(bool active)"),
                ("EMS.Application/Services/TechnicianAssignment/TechnicianAssignmentService.cs", "VisibleRows()"),
                ("EMS.Infrastructure/Services/Identity/UserManagementService.Assignments.cs", "VisibleUsers()"),
            };

            foreach (var (file, marker) in sources)
            {
                var line = File.ReadAllLines(Path.Combine(RepositoryRoot(), file)).Single(l => l.Contains(marker) && l.Contains("=>"));
                Assert.True(line.Contains("_access.Apply("), $"{file}: {marker} must go through _access.Apply: {line.Trim()}");
            }
        }
    }
}
