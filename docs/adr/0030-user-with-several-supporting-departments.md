# ADR-0030: یک کاربر می‌تواند در چند ستاد باشد (جدول واسط، نه ستون تکی)

- **وضعیت:** پذیرفته‌شده
- **ثبت‌شده در:** ۲۰۲۶-۱۱-۱۴
- **مرتبط:** [ADR-0028](0028-row-security-central-door-and-policies.md)، [ADR-0029](0029-row-security-rule-and-coverage.md)، [BUSINESS-DECISIONS.md ۳.۲۴](../BUSINESS-DECISIONS.md)

## زمینه
ستاد کاربر ستون تکی `AspNetUsers.SupportingDepartmentId` بود و RLS روی همین فرض ساخته شده بود (`DataScope` با یک شناسه). مدیر تیم گفت رابطه باید یک‌به‌چند باشد و فقط برای «مشاهدهٔ همه» (`DataScope.ViewAll`) نیست؛ ممکن است کسی دو ستاد را ببیند. صاحب محصول شکستن قرارداد API را پذیرفت.

## تصمیم
- **جدول واسط** `UserSupportingDepartments (UserId, SupportingDepartmentId)` با کلید ترکیبی و ایندکس روی `SupportingDepartmentId`. ستون قدیمی حذف می‌شود؛ **ستاد اصلی نداریم** (ستادها برابرند).
- **`DataScope.SupportingDepartmentIds`** (فهرست) به‌جای شناسهٔ تکی. هر پنج سیاست ردیف و سیاست کاربر شرط `departments.Contains(...)` می‌گیرند. فهرست در متغیر محلی گرفته می‌شود تا پارامتر SQL شود (نه متن چسبانده‌شده).
- **خواندن scope:** اعتبارسنجی توکن، پس از معتبربودن مهر امنیتی، ستادهای کاربر را با **یک کوئری ایندکس‌دار** می‌خواند. برای دارندهٔ `DataScope.ViewAll` (که از claim توکن معلوم است) این کوئری زده **نمی‌شود**.
- **سیاست کاربر و تکنسین:** `ApplicationUser` از `u.SupportingDepartments.Any(...)` (EXISTS)؛ `UserDirectoryRow` و `TechnicianInfo` از `SupportingDepartmentIds` که همان زیرکوئری را در projection دارد.
- **API (شکننده):** ورودی `supportingDepartmentIds[]`؛ خروجی هر شخص `supportingDepartments[]` (هر کدام با شهر و استان). `PersonReference`/`PersonReferenceMapper` یک مجموعهٔ ثابت دارند: نام، کد پرسنلی، فعال‌بودن، سمت و فهرست ستادها. ردیف‌های لیست و Excel (تجهیز، گزارش) فقط متن‌های جداشده با «، » را دارند و فهرست ساخت‌یافتهٔ لیست از جست‌وجوی شخص می‌آید (یک کوئری برای کل صفحه).
- **تغییر ستادهای کاربر:** جایگزینی کامل. هر ستادِ افزوده یا برداشته‌شده باید برای ویرایشگر دیدنی باشد و کاربر باید بعد از ویرایش برای او دیدنی بماند (`404 department_not_found`)؛ ستادی که ویرایشگر نمی‌بیند و به آن دست نمی‌زند می‌ماند.
- **حذف:** ردیف واسط حذف فیزیکی می‌شود (بدون `IsActive`)؛ ردّ تغییر در رویداد ممیزی `UserDepartmentsChanged` است. غیرفعال‌کردن ستاد وقتی کاربر فعالی در آن هست مثل پیش از این رد می‌شود (`HasActiveDependentsAsync`).
- **migration:** جدول ساخته می‌شود، ستادِ فعلی هر کاربر **پیش از حذف ستون** کپی می‌شود، بعد ستون و ایندکس و کلید خارجی حذف می‌شوند. Down یک ستاد (کوچک‌ترین شناسه) برمی‌گرداند و بقیه از دست می‌روند. اسکریپت: `docs/sql/UserSupportingDepartments.sql`.

## گزینه‌های بررسی‌شده
- **الف) ستاد اصلی + ستادهای دیگر:** ستون فعلی می‌ماند و `supportingDepartmentId` در پاسخ‌ها به معنی ستاد اصلی؛ API نمی‌شکست. رد شد، چون شکستن API برای صاحب محصول مشکلی نیست و مفهوم «اصلی» یک قاعدهٔ اضافه می‌خواست (باید همیشه عضو مجموعه باشد و DB آن را اجبار نمی‌کند).
- **ذخیرهٔ ستادها در توکن:** رد شد؛ تغییر ستاد باید فوراً اثر کند (ADR-0006) و توکن تا ۱۵ دقیقه کهنه می‌ماند.
- **ستون JSON روی کاربر:** رد شد؛ نمی‌شود روی آن ایندکس گذاشت و `EXISTS` ساده نوشت.

## پیامدها
- **یک کوئری اضافه در هر درخواست** برای کاربر محدود (نه برای `ViewAll`). ایندکس کلید ترکیبی `(UserId, …)` آن را ارزان نگه می‌دارد.
- فرانت باید هم‌زمان عوض شود: `supportingDepartmentId` در ورودی کاربر و در همهٔ پاسخ‌هایی که شخص دارند حذف شد.
- ردیف‌های لیست و Excel تجهیز و گزارش، شهر و استان را بدون تکرار می‌گویند؛ یک ستاد یا چند ستاد در یک شهر یک بار می‌آید.
- «ستاد کاربر» در متن مستندات قدیمی (`BUSINESS-DECISIONS.md` ۳.۲۳ و اصل `RLS-FINDINGS.md`) هنوز مفرد نوشته شده؛ مرجع فعلی ۳.۲۴ و `ROW-SECURITY.md` است.

## ارجاع
`EMS.Domain/Entities/UserSupportingDepartment.cs`، `EMS.Application/Common/DataAccess/DataScope.cs`، `EMS.Application/Security/RowPolicies/OpenRowPolicies.cs`، `EMS.Infrastructure/Security/AccessTokenValidator.cs`، `EMS.Infrastructure/Services/Identity/UserManagementService*.cs`، `EMS.Tests/Integration/RowSecurityPipelineTests.cs`
