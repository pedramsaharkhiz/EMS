using EMS.Application.Common.DataAccess;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.EmployeePhoneNumber;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.EmployeePhoneNumber;
using EMS.Application.Validators.EmployeePhoneNumber;
using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Domain.ReadModels;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Phone = EMS.Domain.Entities.EmployeePhoneNumber;

namespace EMS.Application.Services.EmployeePhoneNumber;

public sealed class EmployeePhoneNumberService(IEmployeePhoneNumberRepository phones, IRepository<Employee> employees,
    IRowAccess access, ICurrentUserService user, IUnitOfWork unitOfWork, IAuditEventLogger audit) : IEmployeePhoneNumberService
{
    private const string Missing = "شماره موبایل یافت نشد.";
    private void Require(string permission)
    {
        if (!user.Permissions.Contains(permission)) throw new ForbiddenException("دسترسی لازم برای مدیریت شماره‌های کارمند را ندارید.");
    }
    private static void Positive(long id)
    {
        if (id <= 0) throw new ValidationException(new[] { new ValidationFailure("id", "شناسه باید بزرگتر از صفر باشد.") });
    }
    private IQueryable<EmployeeContactRow> EmployeeRows() => employees.Query().Select(e => new EmployeeContactRow
    { EmployeeId = e.EmployeeId, SupportingDepartmentId = e.Organization.SupportingDepartmentId });
    private IQueryable<EmployeeContactRow> PhoneRows() => phones.QueryAll().Select(p => new EmployeeContactRow
    { EmployeeId = p.EmployeeId, EmployeePhoneNumberId = p.EmployeePhoneNumberId, SupportingDepartmentId = p.Employee.Organization.SupportingDepartmentId });
    private IQueryable<Phone> VisiblePhones()
    {
        var ids = access.Apply(PhoneRows()).Select(p => p.EmployeePhoneNumberId);
        return phones.QueryAll().Where(p => ids.Contains(p.EmployeePhoneNumberId));
    }
    private Task RequireEmployeeAsync(long id, CancellationToken ct) => access.RequireVisibleAsync(EmployeeRows(), e => e.EmployeeId == id,
        "EmployeeContact", id, "کارمند یافت نشد.", ct);
    private async Task<Phone> LoadAsync(long id, CancellationToken ct)
    {
        Positive(id);
        await access.RequireVisibleAsync(PhoneRows(), p => p.EmployeePhoneNumberId == id, "EmployeePhoneNumber", id, Missing, ct);
        return await phones.QueryAll(tracked: true).SingleAsync(p => p.EmployeePhoneNumberId == id, ct);
    }
    private void Version(Phone phone, string? version, string path = "rowVersion")
    {
        if (!RowVersionToken.TryParse(version, out var expected)) return;
        if (phone.ModifiedDateTime != expected)
            throw new ConflictItemsException(new[] { new ApiErrorDetail { Field = path, Message = RowVersionToken.ConflictMessage } }, RowVersionToken.ConflictMessage, ErrorCodes.ConcurrencyConflict);
        phones.SetExpectedModifiedDateTime(phone, expected);
    }
    private async Task UniqueAsync(string number, long? exceptId, CancellationToken ct)
    {
        // row-access: uniqueness is global, including hidden and inactive records; never expose their owner.
        if (await phones.QueryAll().AnyAsync(p => p.PhoneNumber == number && p.EmployeePhoneNumberId != exceptId, ct))
            throw new ConflictException(EmployeePhoneRules.TakenMessage, EmployeePhoneRules.TakenCode);
    }
    private static EmployeePhoneNumberItemDto Item(Phone p) => new()
    {
        Id = p.EmployeePhoneNumberId, EmployeeId = p.EmployeeId, PhoneNumber = p.PhoneNumber, IsActive = p.IsActive,
        CreatedByUserId = p.CreatedByUserId, CreatedDateTime = p.CreatedDateTime,
        ModifiedByUserId = p.ModifiedByUserId, ModifiedDateTime = p.ModifiedDateTime, RowVersion = RowVersionToken.From(p.ModifiedDateTime)
    };
    public async Task<PagedResult<EmployeePhoneNumberItemDto>> GetPagedAsync(EmployeePhoneNumberListRequestDto request, CancellationToken ct)
    {
        Require(Permissions.EmployeePhoneNumbers.View);
        await new EmployeePhoneNumberListRequestDtoValidator().ValidateAndThrowAsync(request, ct);
        var query = VisiblePhones().Where(p => p.IsActive == (request.IsActive ?? true));
        if (request.EmployeeId is { } employeeId) query = query.Where(p => p.EmployeeId == employeeId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = EmployeePhoneRules.Normalize(request.Search);
            query = query.Where(p => p.PhoneNumber.Contains(search));
        }
        var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = (request.SortBy?.ToLowerInvariant(), descending) switch
        {
            ("phonenumber", true) => query.OrderByDescending(p => p.PhoneNumber),
            ("phonenumber", false) => query.OrderBy(p => p.PhoneNumber),
            ("createddatetime", true) => query.OrderByDescending(p => p.CreatedDateTime),
            ("createddatetime", false) => query.OrderBy(p => p.CreatedDateTime),
            ("modifieddatetime", true) => query.OrderByDescending(p => p.ModifiedDateTime),
            ("modifieddatetime", false) => query.OrderBy(p => p.ModifiedDateTime),
            (_, true) => query.OrderByDescending(p => p.EmployeePhoneNumberId),
            _ => query.OrderBy(p => p.EmployeePhoneNumberId)
        };
        var page = await ordered.ThenBy(p => p.EmployeePhoneNumberId).ToPagedResultAsync(request.PageNumber, request.PageSize, ct);
        return new() { Items = page.Items.Select(Item).ToList(), PageNumber = page.PageNumber, PageSize = page.PageSize, TotalCount = page.TotalCount };
    }
    public async Task<EmployeePhoneNumberItemDto> GetByIdAsync(long id, CancellationToken ct)
    {
        Require(Permissions.EmployeePhoneNumbers.View);
        return Item(await LoadAsync(id, ct));
    }
    public async Task<EmployeePhoneNumberItemDto> CreateAsync(EmployeePhoneNumberCreateDto request, CancellationToken ct)
    {
        Require(Permissions.EmployeePhoneNumbers.Create);
        await new EmployeePhoneNumberCreateDtoValidator().ValidateAndThrowAsync(request, ct);
        await RequireEmployeeAsync(request.EmployeeId, ct);
        var number = EmployeePhoneRules.Normalize(request.PhoneNumber);
        await UniqueAsync(number, null, ct);
        var phone = new Phone { EmployeeId = request.EmployeeId, PhoneNumber = number };
        await phones.AddAsync(phone, ct);
        await unitOfWork.SaveChangesAsync(ct);
        audit.Created("EmployeePhoneNumber", phone.EmployeePhoneNumberId);
        return Item(phone);
    }
    public async Task<EmployeePhoneNumberItemDto> UpdateAsync(long id, EmployeePhoneNumberUpdateDto request, CancellationToken ct)
    {
        Require(Permissions.EmployeePhoneNumbers.Update);
        await new EmployeePhoneNumberUpdateDtoValidator().ValidateAndThrowAsync(request, ct);
        var phone = await LoadAsync(id, ct);
        Version(phone, request.RowVersion);
        var number = EmployeePhoneRules.Normalize(request.PhoneNumber);
        await UniqueAsync(number, id, ct);
        if (phone.PhoneNumber != number || phone.IsActive != request.IsActive)
        {
            phone.PhoneNumber = number; phone.IsActive = request.IsActive;
            phones.Update(phone);
            await unitOfWork.SaveChangesAsync(ct);
            audit.Updated("EmployeePhoneNumber", id);
        }
        return Item(phone);
    }
    public async Task DeleteAsync(long id, string? rowVersion, CancellationToken ct)
    {
        Require(Permissions.EmployeePhoneNumbers.Delete);
        var phone = await LoadAsync(id, ct);
        Version(phone, rowVersion);
        phones.Remove(phone);
        await unitOfWork.SaveChangesAsync(ct);
        audit.Deleted("EmployeePhoneNumber", id);
    }
    public async Task BulkDeleteAsync(EmployeePhoneNumberBulkDeleteDto request, CancellationToken ct)
    {
        Require(Permissions.EmployeePhoneNumbers.Delete);
        await new EmployeePhoneNumberBulkDeleteDtoValidator().ValidateAndThrowAsync(request, ct);
        var records = new List<Phone>();
        var invalid = new List<ApiErrorDetail>(); var conflicts = new List<ApiErrorDetail>();
        for (var i = 0; i < request.Items.Count; i++)
        {
            var input = request.Items[i];
            try
            {
                var phone = await LoadAsync(input.Id, ct);
                Version(phone, input.RowVersion, $"items[{i}].rowVersion");
                records.Add(phone);
            }
            catch (NotFoundException) { invalid.Add(new() { Field = $"items[{i}].id", Message = Missing }); }
            catch (ConflictItemsException ex) { conflicts.AddRange(ex.Details); }
        }
        if (invalid.Count > 0) throw new BulkItemsException(invalid.Concat(conflicts).ToList(), "حذف گروهی انجام نشد؛ اطلاعات ردیف‌های مشخص‌شده را بررسی کنید.");
        if (conflicts.Count > 0) throw new ConflictItemsException(conflicts, "حذف گروهی انجام نشد؛ نسخهٔ رکوردها تغییر کرده است.", ErrorCodes.ConcurrencyConflict);
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
        phones.RemoveRange(records);
        await unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        foreach (var phone in records) audit.Deleted("EmployeePhoneNumber", phone.EmployeePhoneNumberId);
    }
    public async Task ApplyForReportAsync(long? employeeId, List<EmployeePhoneNumberInputDto>? items, CancellationToken ct)
    {
        var errors = EmployeePhoneInputRules.Errors(employeeId, items);
        if (errors.Count > 0) throw new ValidationException(errors);
        if (items is null || items.Count == 0) return;
        if (!unitOfWork.HasActiveTransaction) throw new InvalidOperationException("Phone changes require the report transaction.");
        if (items.Any(i => i.Id is null)) Require(Permissions.EmployeePhoneNumbers.Create);
        if (items.Any(i => i.Id is not null)) Require(Permissions.EmployeePhoneNumbers.Update);
        var owner = employeeId!.Value;
        await RequireEmployeeAsync(owner, ct);
        var numbers = items.Select(i => EmployeePhoneRules.Normalize(i.PhoneNumber)).Distinct().ToArray();
        var ids = items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToArray();
        // row-access: global ownership check; only generic conflict messages are returned for another employee's phone.
        var existing = await phones.QueryAll(tracked: true).Where(p => numbers.Contains(p.PhoneNumber) || ids.Contains(p.EmployeePhoneNumberId)).ToListAsync(ct);
        var updates = new List<(Phone Phone, string Number)>(); var additions = new HashSet<string>();
        var desired = new Dictionary<string, long?>();
        for (var i = 0; i < items.Count; i++)
        {
            var input = items[i]; var number = EmployeePhoneRules.Normalize(input.PhoneNumber);
            var path = $"employeePhoneNumbers[{i}]";
            if (desired.TryGetValue(number, out var previousId) && previousId != input.Id)
                throw new ConflictItemsException(new[] { new ApiErrorDetail { Field = path + ".phoneNumber", Message = "شماره موبایل در لیست ارسالی تکراری است." } }, "شماره‌های ارسالی با هم تعارض دارند.", EmployeePhoneRules.TakenCode);
            desired[number] = input.Id;
            var occupied = existing.FirstOrDefault(p => p.PhoneNumber == number);
            if (input.Id is { } id)
            {
                var phone = existing.FirstOrDefault(p => p.EmployeePhoneNumberId == id && p.EmployeeId == owner)
                    ?? throw new NotFoundException("شمارهٔ انتخاب‌شده برای این کارمند یافت نشد.");
                Version(phone, input.RowVersion, path + ".rowVersion");
                if (occupied is not null && occupied.EmployeePhoneNumberId != id) throw Taken(path);
                updates.Add((phone, number));
            }
            else if (occupied is not null)
            {
                if (occupied.EmployeeId != owner) throw Taken(path);
                updates.Add((occupied, number)); // existing own number is kept, inactive one is reactivated
            }
            else additions.Add(number);
        }
        // All inputs were checked before changing tracked entities; omitted phones are deliberately untouched.
        foreach (var (phone, number) in updates.Distinct())
        {
            if (phone.PhoneNumber == number && phone.IsActive) continue;
            phone.PhoneNumber = number; phone.IsActive = true; phones.Update(phone);
        }
        foreach (var number in additions) await phones.AddAsync(new Phone { EmployeeId = owner, PhoneNumber = number }, ct);
    }
    private static ConflictItemsException Taken(string path) => new(new[] { new ApiErrorDetail { Field = path + ".phoneNumber", Message = EmployeePhoneRules.TakenMessage } }, EmployeePhoneRules.TakenMessage, EmployeePhoneRules.TakenCode);
    public async Task<List<EmployeePhoneNumberItemDto>> GetForReportAsync(long? employeeId, CancellationToken ct)
    {
        if (employeeId is null || !user.Permissions.Contains(Permissions.EmployeePhoneNumbers.View)) return [];
        if (!await access.IsVisibleAsync(EmployeeRows(), p => p.EmployeeId == employeeId, ct)) return [];
        return (await VisiblePhones().Where(p => p.EmployeeId == employeeId && p.IsActive).OrderBy(p => p.EmployeePhoneNumberId).ToListAsync(ct)).Select(Item).ToList();
    }
}
