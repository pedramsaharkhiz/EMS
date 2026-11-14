using EMS.Application.Dtos.User;
using EMS.Application.Validators.User;
using EMS.Domain;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EMS.Tests.Unit;

public class UserIdentityRulesTests
{
    [Theory]
    [InlineData(" ۰۰۱٢۳٤۵۶۷۸ ", "0012345678")]
    [InlineData("\t PC-٠۰۱ \n", "PC-001")]
    public void Normalization_preserves_leading_zeros_and_non_numeric_personnel_codes(string value, string expected) =>
        Assert.Equal(expected, UserIdentityRules.Normalize(value));

    [Theory]
    [InlineData("0000000000")]
    [InlineData("1111111111")]
    [InlineData("9999999999")]
    [InlineData("0012345678")]
    [InlineData("123456789")]
    [InlineData("12345678901")]
    [InlineData("12345x7890")]
    [InlineData("123456789\n")]
    public void Invalid_national_codes_are_rejected(string value) => Assert.False(UserIdentityRules.IsValidNationalCode(value));

    [Fact]
    public void Valid_checksum_and_persian_digits_are_accepted_by_all_user_validators()
    {
        var code = NationalCodeFixture.FromPrefix("001234567");
        Assert.True(UserIdentityRules.IsValidNationalCode(code));
        var persian = string.Concat(code.Select(c => (char)('۰' + c - '0')));
        var create = new CreateUserDto { PersonnelCode = " PC-۱۲ ", NationalCode = " " + persian + " " };
        Assert.Equal("PC-12", create.PersonnelCode);
        Assert.Equal(code, create.NationalCode);
        Assert.DoesNotContain(new CreateUserDtoValidator().Validate(create).Errors, e => e.PropertyName is "NationalCode" or "PersonnelCode");
        Assert.DoesNotContain(new UpdateUserDtoValidator().Validate(new UpdateUserDto() { PersonnelCode = "p", NationalCode = persian }).Errors, e => e.PropertyName is "NationalCode" or "PersonnelCode");
        Assert.DoesNotContain(new UserValidator().Validate(new UserDto() { PersonnelCode = "p", NationalCode = persian }).Errors, e => e.PropertyName is "NationalCode" or "PersonnelCode");
    }

    [Fact]
    public void National_code_is_optional_and_personnel_code_is_required()
    {
        var dto = new CreateUserDto { NationalCode = " \t " };
        Assert.Null(dto.NationalCode);
        var errors = new CreateUserDtoValidator().Validate(dto).Errors;
        Assert.DoesNotContain(errors, e => e.PropertyName == "NationalCode");
        Assert.Contains(errors, e => e.PropertyName == "PersonnelCode" && e.ErrorMessage == "کد پرسنلی الزامی است.");
    }

    [Fact]
    public void Database_indexes_cover_inactive_users_and_allow_multiple_null_national_codes()
    {
        using var db = TestDb.SqlServerModelOnly();
        var entity = db.Model.FindEntityType(typeof(ApplicationUser))!;
        var personnel = entity.GetIndexes().Single(i => i.GetDatabaseName() == UserIdentityRules.PersonnelIndex);
        Assert.True(personnel.IsUnique);
        Assert.Null(personnel.GetFilter());
        var national = entity.GetIndexes().Single(i => i.GetDatabaseName() == UserIdentityRules.NationalIndex);
        Assert.True(national.IsUnique);
        Assert.Equal("[NationalCode] IS NOT NULL", national.GetFilter());
        Assert.Empty(entity.GetDeclaredQueryFilters());
        Assert.Equal("personnel_code_taken", UniqueConstraintMessages.Find(UserIdentityRules.PersonnelIndex)!.Value.Code);
        Assert.Equal("national_code_taken", UniqueConstraintMessages.Find(UserIdentityRules.NationalIndex)!.Value.Code);
    }

    [Fact]
    public async Task Direct_identity_saves_also_normalize_the_stored_values()
    {
        var options = new DbContextOptionsBuilder<EMSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new EMSDbContext(options, TestDb.NewInterceptor());
        var user = new ApplicationUser { UserName = "test", FirstName = "test", LastName = "user", PasswordHash = "test-only", PersonnelCode = " ۰۰۱ ", NationalCode = " ٠٠١٢٣٤٥٦٧٩ " };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var stored = await db.Users.SingleAsync();
        Assert.Equal("001", stored.PersonnelCode);
        Assert.Equal("0012345679", stored.NationalCode);
    }
}
