using EMS.Application.Dtos.User;
using EMS.Application.Validators.User;

namespace EMS.Tests.Unit;

public sealed class UserAssignmentValidationTests
{
    [Fact]
    public void Update_null_assignments_preserve_existing_relations()
    {
        var request = ValidUpdate();

        Assert.Null(request.RoleIds);
        Assert.Null(request.SkillIds);

        Assert.True(
            new UpdateUserDtoValidator()
                .Validate(request)
                .IsValid);
    }

    [Fact]
    public void Update_empty_assignments_are_valid_and_mean_clear_all()
    {
        var request = ValidUpdate();

        request.RoleIds = [];
        request.SkillIds = [];

        Assert.True(
            new UpdateUserDtoValidator()
                .Validate(request)
                .IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_rejects_nonpositive_assignment_ids(long id)
    {
        var request = ValidUpdate();

        request.RoleIds = [id];
        request.SkillIds = [id];

        var result = new UpdateUserDtoValidator()
            .Validate(request);

        Assert.Contains(
            result.Errors,
            x => x.PropertyName.StartsWith("RoleIds"));

        Assert.Contains(
            result.Errors,
            x => x.PropertyName.StartsWith("SkillIds"));
    }

    [Fact]
    public void Update_rejects_duplicate_assignments()
    {
        var request = ValidUpdate();

        request.RoleIds = [1, 1];
        request.SkillIds = [2, 2];

        var result = new UpdateUserDtoValidator()
            .Validate(request);

        Assert.Contains(
            result.Errors,
            x => x.PropertyName == "RoleIds");

        Assert.Contains(
            result.Errors,
            x => x.PropertyName == "SkillIds");
    }

    [Fact]
    public void Create_rejects_duplicate_assignments()
    {
        var request = new CreateUserDto
        {
            UserName = "testuser",
            Password = "Example123!",
            FirstName = "Test",
            LastName = "User",
            PersonnelCode = "100",
            RoleIds = [1, 1],
            SkillIds = [2, 2]
        };

        var result = new CreateUserDtoValidator()
            .Validate(request);

        Assert.Contains(
            result.Errors,
            x => x.PropertyName == "RoleIds");

        Assert.Contains(
            result.Errors,
            x => x.PropertyName == "SkillIds");
    }

    [Fact]
    public void Update_limits_assignment_count()
    {
        var request = ValidUpdate();

        request.SkillIds = Enumerable.Range(1, 101)
            .Select(x => (long)x)
            .ToList();

        Assert.False(
            new UpdateUserDtoValidator()
                .Validate(request)
                .IsValid);
    }

    private static UpdateUserDto ValidUpdate() => new()
    {
        UserName = "testuser",
        FirstName = "Test",
        LastName = "User",
        PersonnelCode = "100"
    };
}
