# خروجی Excel، تاریخ شمسی و فیلتر لیست‌ها

سه ابزار مشترک که هر ماژول بدون نوشتن دوباره‌کاری از آن‌ها استفاده می‌کند. هر کدام یک جا نوشته شده تا همه‌ی برنامه یک‌شکل باشد.

| ابزار | کجا | کار |
|---|---|---|
| `PersianDate` | `EMS.Application/Common/Globalization` | تبدیل تاریخ UTC به متن شمسی با ساعت تهران |
| `FilterBuilder<TEntity, TRequest>` | `EMS.Application/Common/Filtering` | اعلام فیلترهای یک لیست، یک خط برای هر فیلتر |
| `IExcelExporter` + `ExcelExportDefinition<TRow>` | `EMS.Application/Common/Export` (پیاده‌سازی در `EMS.Infrastructure/Services/Export`) | ساخت فایل `.xlsx` استاندارد |

## تاریخ شمسی (`PersianDate`)

- همه‌ی `DateTime`های دیتابیس UTC‌اند. `PersianDate.ToDate(x)` و `ToDateTime(x)` مقدار را به ساعت تهران (`Asia/Tehran`) می‌برند و می‌نویسند `1405/08/12` یا `1405/08/12 14:30` (صفرپر، ارقام لاتین؛ مرتب‌سازی متنی درست و قابل جست‌وجو در Excel).
- `Kind = Unspecified` (مقداری که دیتابیس برمی‌گرداند) UTC فرض می‌شود. `DateOnly` بدون تبدیل منطقه‌ی زمانی. مقدار null رشته‌ی خالی می‌دهد.
- API همچنان ISO 8601 با `Z` می‌دهد. شمسی فقط برای نمایش متنی (Excel) است.
- اگر میزبان داده‌ی منطقه‌ی زمانی نداشته باشد، ثابت UTC+03:30 به‌کار می‌رود (ایران از ۱۴۰۱ ساعت تابستانی ندارد).
- تست `PersianDateTests` تاریخ‌های نوروز و سال‌های کبیسه را با تقویم رسمی مقایسه می‌کند. اگر روزی `PersianCalendar` خود .NET با تقویم رسمی اختلاف داشت، تست می‌شکند و الگوریتم پشت همین کلاس عوض می‌شود.
- هشدار برای تست‌ها: اگر فرهنگ ماشین فارسی باشد، `DateTime.Parse("2021-03-20")` سال ۲۰۲۱ را «شمسی» می‌خواند. همیشه `CultureInfo.InvariantCulture` بده.

## فیلتر لیست‌ها (`FilterBuilder`)

مثل `SortBuilder` برای مرتب‌سازی. هر فیلتر یک خط است و فقط وقتی اعمال می‌شود که مقدارش فرستاده شده باشد:

```csharp
private static readonly FilterBuilder<Equipment, EquipmentListRequestDto> Filters = new FilterBuilder<Equipment, EquipmentListRequestDto>()
    .Search(r => r.Search, x => x.YaasCode, x => x.SerialNumber, x => x.AssetNumber)   // هرکدام شامل متن
    .AnyOf(r => r.StatusIds, x => x.EquipmentStatusId)                                  // چندمقداری
    .Eq(r => r.OrganizationId, x => x.CurrentOrganizationId)                            // مساوی
    .AtLeast(r => r.CreatedFrom, x => x.CreatedDateTime)                                // از (شامل)
    .AtMost(r => r.CreatedTo, x => x.CreatedDateTime);                                  // تا (شامل)
```

- `Eq`، `AnyOf`، `Contains`، `Search`، `AtLeast`، `AtMost` برای ستون‌های ساده و nullable کار می‌کنند.
- فیلتر پیچیده‌تر (مثلاً «نوع مؤثر») با `Add(request => expression)` نوشته می‌شود؛ اگر درخواست آن را نخواسته باشد `null` برگردان.
- مقدارها پارامتر SQL‌اند (نه رشته‌ی چسبانده‌شده).
- لیست و Excel از **همین** `Filters.Apply(...)` استفاده می‌کنند تا هرگز ناهماهنگ نشوند.

## خروجی Excel

### استفاده در یک ماژول (۳ کار)

1. یک تعریف بنویس (ستون‌ها، عنوان‌های فارسی):

```csharp
public static class EquipmentExport
{
    public static readonly ExcelExportDefinition<EquipmentRow> Definition = ExcelExportDefinition
        .For<EquipmentRow>(name: "Equipment", sheetTitle: "تجهیزات", fileLabel: "تجهیزات")
        .Column(x => x.Id)                                  // عنوان «شناسه» از دیکشنری مشترک
        .Column(x => x.YaasCode, "یاس کد", ExcelCellKind.Text)
        .Column(x => x.StatusTitle, "وضعیت")
        .Column(x => x.CreatedDateTime)                     // تاریخ: خودکار شمسی
        .Build();
}
```

2. در کنترلر یک action با `[ExcelResponse]` که `Task<IActionResult>` برمی‌گرداند و از `ExcelDownload(...)` استفاده می‌کند.
3. سرویس `IExcelExporter.ExportAsync(query, Definition)` را صدا می‌زند. `query` همان کوئری فیلترشده‌ی لیست (بدون صفحه‌بندی) است.

> ماژول‌هایی که خروجی دارند: تجهیز (`EquipmentExport`)، گزارش خرابی (`FailureReportExport`) و کاربران (`UserExport`). قاعدهٔ مشترک: **خروجی و لیست از یک کوئری ساخته می‌شوند** ([ADR-0021](adr/0021-list-and-export-share-one-row-source.md)).

**کاربران (`GET /api/Users/export`، مجوز `Users.View`):** همان فیلترهای لیست کاربران (`search`، `roleId`، `skillId`، `hasRole`، `hasSkill`، `isActive`، ...؛ صفحه‌بندی نادیده گرفته می‌شود، ترتیب مثل لیست بر اساس شناسه). ستون‌ها همان چیزهایی‌اند که لیست نشان می‌دهد: نام کاربری، نام، نام خانوادگی، کد پرسنلی، کد ملی، تلفن، ایمیل، سمت، دپارتمان پشتیبانی، شهر، استان، نقش‌ها، مهارت‌ها (هر کدام در یک سلول، مرتب و با «، » جدا) و فعال. کد پرسنلی، کد ملی، تلفن و نام کاربری **متن**‌اند تا صفرهای ابتدایی بمانند. **هیچ دادهٔ امنیتی** (رمز، هش، stamp) در فایل یا حتی در کوئری نیست. نقطهٔ ورود واحد ردیف‌ها: `VisibleUsers()` در `UserManagementService` (لیست و فایل هر دو از `BuildUserQuery` می‌آیند؛ جزئیات کاربر هم باید بعداً همان قاعدهٔ دید را بگیرد).

### چه چیزی خودکار است

- شیت **راست‌به‌چپ** با نام فارسی (حداکثر ۳۱ کاراکتر، کاراکترهای ممنوع حذف می‌شود)، فونت Tahoma، هدر پررنگ و ثابت با فیلتر، عرض ستون متناسب با محتوا.
- مقدارها: `bool` ← «بله/خیر»، `DateTime` ← شمسی با ساعت تهران (`ExcelCellKind.Date` فقط تاریخ)، عدد ← عدد، enum ← نام، `null` ← سلول خالی.
- **`ExcelCellKind.Text`** برای کدها و سریال‌ها: صفرهای ابتدایی از بین نمی‌رود.
- متنی که با `= + - @` شروع شود هیچ‌وقت فرمول نمی‌شود: سلول متنی با quote prefix نوشته می‌شود (خود داده تغییر نمی‌کند).
- نام فایل: `<fileLabel>-<تاریخ شمسی امروز>.xlsx`، مثلاً `تجهیزات-1405-08-12.xlsx`.
- هر خروجی در audit log ثبت می‌شود (`Exported`، نام موجودیت، تعداد ردیف). خود داده ثبت نمی‌شود.

### عنوان‌های فارسی ستون‌ها

۱. عنوانی که در تعریف می‌دهی همیشه برنده است. ۲. اگر ندادی، از `CommonColumnTitles` (دیکشنری مشترک: `Id`، `Code`، `Title`، `Description`، `IsActive`، `CreatedDateTime`، `ModifiedDateTime`) گرفته می‌شود. ۳. اگر آنجا هم نبود، `Build()` همان‌جا خطا می‌دهد؛ ستون بدون عنوان یا دو ستون با عنوان یکسان هرگز وارد فایل نمی‌شود. نام‌هایی که در هر موجودیت معنای دیگری دارند را به دیکشنری اضافه نکن؛ در تعریف خودت عنوان بده. برای چندزبانه‌شدن، `IColumnTitleProvider` را با یک منبع دیگر پیاده کن.

### محدودیت‌ها (`appsettings.json`، بخش `Export`)

| تنظیم | پیش‌فرض | معنی |
|---|---|---|
| `MaxRows` | 50000 | بیشتر از این `400 export_too_large` (پیام شامل تعداد واقعی)؛ نمی‌بُرد |
| `MaxConcurrent` | 2 | بیشتر از این هم‌زمان `429 export_busy` |

اندازه‌گیری روی ۵۰ هزار ردیف و ۱۷ ستون: حدود ۲٫۶ ثانیه، فایل ۳ مگابایت، حدود ۱۸۰ مگابایت حافظه‌ی مدیریت‌شده (روی یک ماشین توسعه). با دو خروجی هم‌زمان بدترین حالت حدود ۴۰۰ مگابایت است. اگر روزی بیشتر لازم شد، فقط پیاده‌سازی `ExcelExporter` به یک روش جریانی (مثل OpenXML SDK) عوض می‌شود و هیچ ماژولی دست نمی‌خورد.

### نوشتن تست برای یک خروجی

نمونه‌ی واقعی: `EquipmentExport` و `EquipmentTests` (فایل را با ClosedXML دوباره می‌خواند). تعریف را `Build()` کن (تست می‌کند عنوان‌ها کامل و بدون تکرارند) و فایل را با ClosedXML دوباره بخوان؛ `ExcelExportTests` الگوی کامل است.
