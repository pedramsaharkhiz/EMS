using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EMS.Tests.Unit;

public class AssignmentNoteModelTests
{
    [Fact]
    public void Note_has_bounded_unicode_text_foreign_keys_soft_delete_and_optimistic_concurrency()
    {
        using var db = TestDb.SqlServerModelOnly();
        var note = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(AssignmentNote))!;
        Assert.Equal("AssignmentNotes", note.GetTableName());
        Assert.Equal(AssignmentNoteConstraints.TextMaxLength, note.FindProperty(nameof(AssignmentNote.Text))!.GetMaxLength());
        Assert.False(note.FindProperty(nameof(AssignmentNote.Text))!.IsNullable);
        Assert.True(note.FindProperty(nameof(AssignmentNote.SeenDateTime))!.IsNullable);
        Assert.True(note.FindProperty(nameof(AssignmentNote.ModifiedDateTime))!.IsConcurrencyToken);
        Assert.Equal(new[] { "CreatedByUserId", "NoteCategoryId", "TechnicianAssignmentId" },
            note.GetForeignKeys().SelectMany(x => x.Properties).Select(x => x.Name).OrderBy(x => x));
        Assert.All(note.GetForeignKeys(), x => Assert.Equal(DeleteBehavior.NoAction, x.DeleteBehavior));
        Assert.Contains(note.GetDeclaredQueryFilters(), x => x.Key == "SoftDelete");
    }
}
