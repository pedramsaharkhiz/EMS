using System.Text.RegularExpressions;

namespace EMS.Tests.Unit
{
    /// <summary>
    /// Guards that protected rows are only ever reached through the central door. The services get their rows from ONE method each (VisibleRows / VisibleUsers) that
    /// goes through IRowAccess, and a record reached by its id (a read or a load for update) must be checked with IRowAccess BEFORE it is loaded; these tests read the
    /// source and refuse a new read or write that bypasses the door. A load that is deliberately unchecked says why in a comment containing "row-access:".
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
                .Where(l => !l.Text.Contains("_access."))
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

        [Fact]
        public void A_record_reached_by_its_id_is_checked_with_the_row_door_before_it_is_loaded()
        {
            // The calls that load one record (or several) of a protected type. Each must sit in a method that asks IRowAccess first (EnsureVisibleAsync / RequireVisibleAsync
            // / RequireVisibleUserAsync / EnsureReportVisibleAsync / _access.), or say why not with a "row-access:" comment on the same line or in the method.
            var files = new[]
            {
                "EMS.Application/Services/Equipment/EquipmentService.cs",
                "EMS.Application/Services/FailureReport/FailureReportService.cs",
                "EMS.Application/Services/TechnicianAssignment/TechnicianAssignmentService.cs",
                "EMS.Infrastructure/Services/Identity/UserManagementService.cs",
                "EMS.Infrastructure/Services/Identity/UserManagementService.Assignments.cs",
                "EMS.Infrastructure/Services/Identity/UserRoleAssignmentService.cs",
            };
            var loads = new[] { "GetRowAsync(", "GetForUpdateAsync(", "GetManyForUpdateAsync(", "_userManager.Users" };
            var checks = new[] { "EnsureVisibleAsync(", "RequireVisibleAsync(", "RequireVisibleUserAsync(", "EnsureReportVisibleAsync(", "_access.", "row-access:" };
            var signature = new Regex(@"^\s{4,8}(public|private|protected|internal)\b[^=;]*\(");

            var offenders = new List<string>();
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(Path.Combine(RepositoryRoot(), file));
                var start = 0;
                for (var i = 0; i < lines.Length; i++)
                {
                    if (signature.IsMatch(lines[i]))
                        start = i;
                    var text = lines[i].TrimStart();
                    if (text.StartsWith("//") || !loads.Any(lines[i].Contains))
                        continue;

                    var method = string.Join("\n", lines[start..(i + 1)]);
                    if (!checks.Any(method.Contains))
                        offenders.Add($"{file}:{i + 1}: {text}");
                }
            }

            Assert.True(offenders.Count == 0, "These loads of a record by its id do not ask IRowAccess first:\n" + string.Join("\n", offenders));
        }
    }
}