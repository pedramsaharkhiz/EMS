# کاتالوگ‌ها (داده‌های پایه): ساختار و افزودن یک مورد جدید

«کاتالوگ» یعنی یک entity از نوع جدول‌های پایه (برند، وضعیت تجهیز، دسته‌بندی و ...): یک `Title` یکتا بین رکوردهای فعال، حذف منطقی با `IsActive`، و CRUD استاندارد. **همه‌ی رفتار CRUD یک‌بار نوشته شده** و هر کاتالوگ فقط تفاوت‌های خودش را اعلام می‌کند.

## معماری

| بخش | کجا | کار |
|---|---|---|
| `CatalogService<...>` | `EMS.Application/Services` | صفحه‌بندی، مرتب‌سازی، فیلتر عنوان، یکتایی عنوان، حذف و بازگردانی، بررسی وابسته‌ها، `rowVersion`، audit و کد خطا |
| `CatalogRepository<T>` | `EMS.Infrastructure/Repositories` | خواندن با شناسه، کوئری فعال/غیرفعال، بررسی عنوان. هر repository فقط `HasActiveDependentsAsync` را می‌نویسد |
| `ICatalogEntity` | `EMS.Domain/Interfaces` | علامت‌گذاری entity (دارای `Title`) |
| validatorهای عمومی | `EMS.Application/Validators/Catalog` | قواعد مشترک list و create و update (هر validator یک خط) |
| `CatalogConstraints` | `EMS.Domain` | حداکثر طول `Title` و `Code`، مشترک بین ستون دیتابیس و validatorها |
| `BaseEntityConvention` | `EMS.Infrastructure/Persistence/Conventions` | ستون‌های مشترک `IsActive` و `CreatedByUserId` و `CreatedDateTime` برای همه‌ی entityها |
| `SoftDelete` | `EMS.Infrastructure/Persistence` | فیلتر سراسری EF برای حذف منطقی |
| کنترلر | `EMS.Presentation/Controllers` | نازک و صریح: هر action یک خط به سرویس |

DTOها **برای هر entity جدا** می‌مانند، چون نام فیلدها بخشی از قرارداد API است (`brandId` در برابر `equipmentStatusId`). validatorها هم کلاس جدا دارند، ولی هرکدام یک خط است و قواعد مشترک (طول عنوان و کد، شناسه‌ی مثبت، محدوده‌ی صفحه‌بندی و جهت مرتب‌سازی) را از کلاس پایه می‌گیرند.

### قواعدی که همه‌ی کاتالوگ‌ها دارند
- عنوان بین رکوردهای **فعال** یکتاست (بدون حساسیت به حروف). رکورد غیرفعال عنوانش را اشغال نمی‌کند. ایندکس یکتا روی دیتابیس هم فیلتردار است (`WHERE IsActive = 1`).
- لیست پیش‌فرض فقط فعال‌ها را می‌دهد. `?isActive=false` فقط غیرفعال‌ها را. `GET /{id}` غیرفعال را هم برمی‌گرداند.
- رکوردی که وابسته‌ی **فعال** دارد غیرفعال نمی‌شود (`409 has_active_dependents`).
- ساخت یا ویرایش زیر والد غیرفعال `400 parent_inactive` و والد ناموجود `404 parent_not_found` می‌دهد.
- `rowVersion` اختیاری برای تشخیص ویرایش هم‌زمان (`409 concurrency_conflict`).
- فیلد متنی در ویرایش: مقدار خالی یا ارسال‌نشده یعنی بدون تغییر، در غیر این صورت trim می‌شود.

## حذف منطقی به‌صورت فیلتر سراسری

`builder.HasSoftDeleteFilter()` در تنظیمات EF هر کاتالوگ، رکوردهای `IsActive = false` را از **همه‌ی کوئری‌های** آن entity حذف می‌کند، بدون اینکه کسی در هر کوئری `.Where(x => x.IsActive)` بنویسد. برای دیدن رکوردهای غیرفعال: `.IncludingInactive()`.

- `CatalogRepository` در startup بررسی می‌کند که entity فیلتر را دارد و تست `Every_catalog_entity_has_the_soft_delete_filter` هم همین را برای همه‌ی `ICatalogEntity`ها تضمین می‌کند.
- **عمداً فقط برای کاتالوگ‌ها اعمال شده، نه همه‌ی entityها.** خطر: entityی که فیلتر ندارد (مثلاً `Equipment`) ولی به رکوردی ارجاع دارد که فیلتر دارد (مثلاً `EquipmentStatus`)، در `Include` ردیف‌هایش را وقتی رکورد ارجاع‌شده غیرفعال شده، بی‌صدا از نتیجه حذف می‌کند. کوئری‌هایی که این `Include`ها را دارند و باید تاریخچه را نشان دهند باید `.IncludingInactive()` بزنند (مثل `EquipmentRepository.GetDetailsAsync`، با تست).
- هشدار EF درباره‌ی همین الگو عمداً خاموش شده، چون یک رکورد فعال هرگز به ردیف غیرفعال یک کاتالوگ ارجاع ندارد (غیرفعال‌سازی با وجود وابسته‌ی فعال رد می‌شود).
- فیلتر هیچ تغییر schema ندارد.

## افزودن یک کاتالوگ جدید

### ۱. تولید فایل‌ها با template

یک‌بار نصب (هر توسعه‌دهنده):

```bash
dotnet new install ./templates/catalog
```

سپس از ریشه‌ی ریپو (نام entity به انگلیسی، `Label` نام فارسی برای پیام‌ها):

```bash
dotnet new ems-catalog -n Manufacturer --Label "سازنده"
# دسته‌بندی: جمع بی‌قاعده و بدون کد
dotnet new ems-catalog -n Category --Label "دسته‌بندی" --Plural Categories --HasCode false
```

۱۶ فایل ساخته می‌شود: entity، تنظیمات EF، repository و interface، ۵ DTO، ۳ validator (یک‌خطی)، سرویس و interface و کنترلر. `Plural` برای نام جدول و گروه permission است (پیش‌فرض: نام + `s`).

### ۲. مراحل دستی

1. **Permission:** در `EMS.Application/Common/Security/Permissions.cs` یک کلاس `Manufacturers` با چهار ثابت `View/Create/Update/Delete` اضافه کنید (الگوی کلاس‌های موجود). `Permissions.All` خودکار از روی ثابت‌ها ساخته می‌شود و جای دیگری ثبت لازم نیست. تا این کار را نکنید build نمی‌شود (کنترلر به آن ارجاع دارد).
2. **Migration:**

   ```bash
   dotnet ef migrations add AddManufacturer --project EMS.Infrastructure --startup-project EMS.Presentation
   ```

   اگر فراموش کنید، تست `SchemaDriftTests` شکست می‌خورد (مدل EF را با آخرین migration مقایسه می‌کند).
3. **وابسته‌ها:** اگر رکورد دیگری به این کاتالوگ ارجاع می‌دهد، `HasActiveDependentsAsync` را در repository پر کنید (پیش‌فرض template فقط `false` برمی‌گرداند) و متن `DependentsMessage` را در سرویس بنویسید.
4. **والد (اختیاری):** اگر به کاتالوگ دیگری وابسته است، repository والد را به سازنده‌ی سرویس بدهید و از `RequireParentAsync` در `BeforeCreateAsync` و `BeforeUpdateAsync` استفاده کنید (نمونه: `EquipmentTypeService`).
5. **فیلتر لیست یا ستون مرتب‌سازی اضافه:** `ApplyFilters` و `ConfigureSorting` را در سرویس بنویسید.
6. **تست:** یک `CatalogCase` در `EMS.Tests/Integration/CatalogCases.cs` اضافه کنید. بعد همه‌ی تست‌های رفتاری کاتالوگ (حدود ۲۰ سناریو: ایجاد، تکراری، لیست، مرتب‌سازی، ویرایش، rowVersion، حذف و بازگردانی، permission، ...) خودکار روی آن هم اجرا می‌شوند.
7. **قرارداد API:** snapshot را عمداً به‌روز کنید:

   ```bash
   UPDATE_API_CONTRACT=1 dotnet test EMS.Tests --filter ApiContractSnapshotTests
   ```

   و تغییر `api-contract.baseline.txt` را در PR بررسی کنید.

### راستی‌آزمایی template
یک entity با کد (`Gadget`) و یکی بدون کد (`Gizmo`) با همین template تولید شد. فقط گروه permission و `CatalogCase` دستی اضافه شد و هر دو کل تست‌های رفتاری کاتالوگ را بدون هیچ کد دستی دیگری گذراندند.

## وقتی کاتالوگ‌مانند نیست
`Equipment`، `FailureReport` و مانند آن‌ها منطق واقعی دارند (وضعیت، گردش کار، ارجاع) و عمداً از این الگو بیرون‌اند. `CatalogService` متدهای `virtual` دارد، ولی اگر یک entity بیشتر از چند hook نیاز دارد، سرویس مستقل بنویسید.
