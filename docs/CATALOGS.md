# کاتالوگ‌ها (داده‌های پایه): ساختار، قواعد و افزودن یک مورد جدید

«کاتالوگ» یعنی یک entity از نوع جدول‌های پایه (برند، وضعیت تجهیز، دسته‌بندی و ...): یک `Title` یکتا بین رکوردهای فعال، حذف منطقی با `IsActive`، و CRUD استاندارد. **همه‌ی رفتار CRUD یک‌بار نوشته شده** (`CatalogService`) و هر کاتالوگ فقط تفاوت‌های خودش را اعلام می‌کند.

## فهرست

1. [آیا entity من کاتالوگ است؟](#۱-آیا-entity-من-کاتالوگ-است)
2. [معماری](#۲-معماری)
3. [قرارداد API هر کاتالوگ](#۳-قرارداد-api-هر-کاتالوگ)
4. [قواعد مشترک همه‌ی کاتالوگ‌ها](#۴-قواعد-مشترک-همهی-کاتالوگها)
5. [حذف منطقی به‌صورت فیلتر سراسری](#۵-حذف-منطقی-به‌صورت-فیلتر-سراسری)
6. [افزودن یک کاتالوگ جدید (مرجع مراحل)](#۶-افزودن-یک-کاتالوگ-جدید-مرجع-مراحل)
7. [مثال کامل از صفر تا صد: کاتالوگ «سازنده»](#۷-مثال-کامل-از-صفر-تا-صد-کاتالوگ-سازنده)
8. [تبدیل یک entity موجود به کاتالوگ (مثال: Skill)](#۸-تبدیل-یک-entity-موجود-به-کاتالوگ-مثال-skill)
9. [عیب‌یابی](#۹-عیبیابی)
10. [وقتی کاتالوگ‌مانند نیست](#۱۰-وقتی-کاتالوگمانند-نیست)

---

## ۱. آیا entity من کاتالوگ است؟

کاتالوگ بودن یعنی **همه‌ی** این‌ها درست باشد:

- یک **عنوان** (`Title`) دارد که بین رکوردهای فعال تکراری نیست.
- «حذف» آن یعنی **غیرفعال‌سازی** (`IsActive = false`)، نه پاک شدن رکورد.
- عملیاتش همان ایجاد، ویرایش، لیست (صفحه‌بندی و فیلتر و مرتب‌سازی) و حذف است، بدون گردش کار یا وضعیت‌های پیچیده.

مثال‌های کاتالوگ: برند، وضعیت تجهیز، دسته‌بندی تجهیز، نوع مکان، مهارت.
مثال‌های غیر کاتالوگ: تجهیز (`Equipment`)، گزارش خرابی (`FailureReport`)، پیام. این‌ها فیلدها و منطق واقعی دارند و عنوان یکتا ندارند. برایشان template کاتالوگ را استفاده نکن ([بخش ۱۰](#۱۰-وقتی-کاتالوگمانند-نیست)).

---

## ۲. معماری

| بخش | کجا | کار |
|---|---|---|
| `CatalogService<...>` | `EMS.Application/Services` | صفحه‌بندی، مرتب‌سازی، فیلتر عنوان، یکتایی عنوان، حذف و بازگردانی، بررسی وابسته‌ها، `rowVersion`، audit و کد خطا |
| `CatalogRepository<T>` | `EMS.Infrastructure/Repositories` | خواندن با شناسه، کوئری فعال/غیرفعال، بررسی عنوان. هر repository فقط `HasActiveDependentsAsync` را می‌نویسد |
| `ICatalogEntity` | `EMS.Domain/Interfaces` | علامت‌گذاری entity (دارای `Title`) |
| validatorهای عمومی | `EMS.Application/Validators/Catalog` | قواعد مشترک list و create و update (هر validator یک خط) |
| `CatalogConstraints` | `EMS.Domain` | حداکثر طول `Title` (100) و `Code` (20)، مشترک بین ستون دیتابیس و validatorها |
| `BaseEntityConvention` | `EMS.Infrastructure/Persistence/Conventions` | ستون‌های مشترک `IsActive` و `CreatedByUserId` و `CreatedDateTime` برای همه‌ی entityها |
| `SoftDelete` | `EMS.Infrastructure/Persistence` | فیلتر سراسری EF برای حذف منطقی |
| کنترلر | `EMS.Presentation/Controllers` | نازک و صریح: هر action یک خط به سرویس |

### مسیر یک درخواست

```
درخواست HTTP
   ↓  Controller        بررسی permission، فقط سرویس را صدا می‌زند
   ↓  validator          (FluentValidation، خودکار پیش از رسیدن به action) طول و الزام فیلدها
   ↓  XxxService         رفتار مشترک از CatalogService + تفاوت‌های این کاتالوگ
   ↓  XxxRepository      کوئری با EF روی EMSDbContext
   ↓  SQL Server
```

DTOها **برای هر entity جدا** می‌مانند، چون نام فیلدها بخشی از قرارداد API است (`brandId` در برابر `equipmentStatusId`). validatorها هم کلاس جدا دارند، ولی هرکدام یک خط است و قواعد مشترک (طول عنوان و کد، شناسه‌ی مثبت، محدوده‌ی صفحه‌بندی و جهت مرتب‌سازی) را از کلاس پایه می‌گیرند.

### چه چیزی خودکار ثبت می‌شود

نیازی به ثبت دستی هیچ‌کدام در DI یا EF نیست:

| چیز | چطور پیدا می‌شود |
|---|---|
| repository | کلاسی که نامش به `Repository` ختم شود و interface هم‌نام دارد (`XxxRepository` ← `IXxxRepository`) |
| سرویس | کلاسی که نامش به `Service` ختم شود (با interfaceهایش) |
| validator | تمام validatorهای اسمبلی `EMS.Application` |
| تنظیمات EF | تمام `IEntityTypeConfiguration`‌ها در اسمبلی `EMS.Infrastructure` |
| permission | `Permissions.All` از روی ثابت‌های کلاس‌های `Permissions` ساخته می‌شود و `IdentitySeeder` آن‌ها را به نقش ادمین می‌دهد |

پس اگر نام‌گذاری از الگو بیرون برود (مثلاً سرویس‌ای که به `Manager` ختم شود)، چیزی ثبت نمی‌شود و وقتی بخواهی استفاده‌اش کنی خطای «سرویس ثبت نشده» می‌گیری.

---

## ۳. قرارداد API هر کاتالوگ

هر کاتالوگ پنج endpoint دارد. مسیر از نام کنترلر می‌آید: `/api/<Entity>` (مثلاً `/api/Brand`). همه به permission نیاز دارند (`<Plural>.View/Create/Update/Delete`، مثلاً `Brands.Create`) و بدون توکن `401` می‌گیرند.

| متد و مسیر | permission | موفقیت | کار |
|---|---|---|---|
| `GET /api/Brand` | `Brands.View` | `200` | لیست صفحه‌بندی‌شده |
| `GET /api/Brand/{id}` | `Brands.View` | `200` | یک رکورد (فعال یا غیرفعال) |
| `POST /api/Brand` | `Brands.Create` | `201` + هدر `Location` | ایجاد |
| `PUT /api/Brand` | `Brands.Update` | `200` | ویرایش (شناسه داخل بدنه است) |
| `DELETE /api/Brand/{id}` | `Brands.Delete` | `204` | حذف منطقی |

### لیست: `GET /api/Brand`

پارامترهای query (همه اختیاری):

| پارامتر | پیش‌فرض | توضیح |
|---|---|---|
| `pageNumber` | `1` | حداقل 1 |
| `pageSize` | `10` | بین 1 تا 100 |
| `sortBy` | شناسه | `createddatetime` یا ستون‌هایی که کاتالوگ اعلام کرده (معمولاً `title` و `code`). نام ناشناخته بی‌صدا نادیده گرفته می‌شود و مرتب‌سازی پیش‌فرض (شناسه) می‌ماند |
| `sortDirection` | `asc` | فقط `asc` یا `desc` |
| `title` | | جستجوی «شامل» روی عنوان |
| `code` | | جستجوی «شامل» روی کد (فقط کاتالوگ‌های دارای کد) |
| `isActive` | فقط فعال‌ها | `false` فقط غیرفعال‌ها را می‌دهد (برای پیدا کردن و بازگرداندن) |

پاسخ:

```json
{
  "items": [
    { "brandId": 3, "title": "سامسونگ", "code": "SMS", "isActive": true }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

### ایجاد: `POST /api/Brand`

```json
{ "title": "سامسونگ", "code": "SMS" }
```

پاسخ `201` شکل «Detail» را برمی‌گرداند:

```json
{ "brandId": 3, "title": "سامسونگ", "code": "SMS", "isActive": true, "rowVersion": "…" }
```

`title` و `code` قبل از ذخیره trim می‌شوند.

### ویرایش: `PUT /api/Brand`

```json
{ "brandId": 3, "title": "سامسونگ الکترونیکس", "code": "SMS", "isActive": true, "rowVersion": "…" }
```

نکته‌های مهم رفتار ویرایش:

- **فیلد متنی خالی یا ارسال‌نشده یعنی «بدون تغییر»**، نه «پاک کن». با ویرایش نمی‌شود `code` را خالی کرد.
- **`isActive` اگر ارسال نشود `true` فرض می‌شود.** پس `PUT` روی رکورد غیرفعال بدون این فیلد، آن را **بازمی‌گرداند**. برای غیرفعال کردن از `DELETE` یا `"isActive": false` استفاده کن.
- بازگرداندن یک رکورد غیرفعال هم همین `PUT` با `"isActive": true` است. اگر عنوانش در این فاصله به رکورد فعال دیگری رسیده باشد، `409 duplicate_title` می‌گیری.
- `rowVersion` اختیاری است ([بخش rowVersion](#rowversion-تشخیص-ویرایش-همزمان)).

### حذف: `DELETE /api/Brand/{id}`

حذف منطقی است: رکورد `IsActive = false` می‌شود و پاک نمی‌شود. `204` برمی‌گرداند. اگر رکورد وابسته‌ی فعال داشته باشد `409 has_active_dependents`.

### کدهای خطایی که کاتالوگ‌ها برمی‌گردانند

قالب کامل خطاها در [ERRORS.md](ERRORS.md) است. کلاینت باید روی `code` تصمیم بگیرد، نه متن پیام.

| status | code | وقتی |
|---|---|---|
| 400 | `validation_failed` | عنوان خالی یا بلندتر از 100، کد بلندتر از 20، `pageSize` بیرون از 1 تا 100، شناسه‌ی غیرمثبت، `sortDirection` نامعتبر. فهرست مشکل‌ها در `errors` است |
| 400 | `invalid_row_version` | `rowVersion` عدد معتبر نیست |
| 400 | `parent_inactive` | والد انتخاب‌شده غیرفعال است |
| 401 / 403 | `unauthorized` / `forbidden` | توکن نیست یا permission لازم را ندارد |
| 404 | `not_found` | رکورد با این شناسه نیست |
| 404 | `parent_not_found` | والد با این شناسه نیست |
| 409 | `duplicate_title` | عنوان دیگری فعال با همین عنوان هست (بدون حساسیت به حروف) |
| 409 | `has_active_dependents` | رکورد وابسته‌ی فعال دارد و غیرفعال نمی‌شود |
| 409 | `concurrency_conflict` | `rowVersion` قدیمی است، رکورد در این فاصله عوض شده |

---

## ۴. قواعد مشترک همه‌ی کاتالوگ‌ها

- **عنوان بین رکوردهای فعال یکتاست** (بدون حساسیت به حروف). رکورد غیرفعال عنوانش را اشغال نمی‌کند. ایندکس یکتا روی دیتابیس هم فیلتردار است (`WHERE IsActive = 1`)، پس اگر کسی از مسیر دیگری (مثلاً مستقیم SQL) عنوان تکراری بنویسد، دیتابیس مانع می‌شود.
- لیست پیش‌فرض فقط فعال‌ها را می‌دهد. `?isActive=false` فقط غیرفعال‌ها را. `GET /{id}` غیرفعال را هم برمی‌گرداند.
- رکوردی که وابسته‌ی **فعال** دارد غیرفعال نمی‌شود (`409 has_active_dependents`). وابسته‌ی غیرفعال مانع نیست.
- ساخت یا ویرایش زیر والد غیرفعال `400 parent_inactive` و والد ناموجود `404 parent_not_found` می‌دهد.
- `rowVersion` اختیاری برای تشخیص ویرایش هم‌زمان (`409 concurrency_conflict`).
- فیلد متنی در ویرایش: مقدار خالی یا ارسال‌نشده یعنی بدون تغییر، در غیر این صورت trim می‌شود.
- ایجاد، ویرایش و حذف هر کدام یک رویداد audit ثبت می‌کنند (جزئیات در [LOGGING.md](LOGGING.md)).

### rowVersion: تشخیص ویرایش هم‌زمان

مشکلی که حل می‌کند: دو کاربر هم‌زمان یک رکورد را باز می‌کنند. اولی ذخیره می‌کند. دومی بدون اینکه تغییر اولی را ببیند ذخیره می‌کند و آن را بی‌صدا بازنویسی می‌کند.

راه‌حل:

1. هر پاسخ Detail یک `rowVersion` دارد (از زمان آخرین ویرایش ساخته می‌شود، ستون جدیدی لازم نیست).
2. کلاینت آن را در `PUT` برمی‌گرداند.
3. اگر رکورد بعد از خواندن عوض شده باشد، `409 concurrency_conflict` برمی‌گردد. کلاینت دوباره می‌خواند و تصمیم می‌گیرد.
4. اگر `rowVersion` فرستاده نشود، بررسی انجام نمی‌شود و آخرین ذخیره برنده است.

برای همین است که در تنظیمات EF هر کاتالوگ خط `builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken()` هست.

### وابسته‌ها (`HasActiveDependentsAsync`)

برای جلوگیری از اینکه رکوردی که جای دیگری استفاده می‌شود غیرفعال شود. مثال برای `Brand`: مدل تجهیزی که به این برند ارجاع دارد و فعال است، مانع غیرفعال شدن برند می‌شود:

```csharp
public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
    => await _context.EquipmentModels.AnyAsync(x => x.BrandId == id && x.IsActive, cancellationToken);
```

متن خطایی که کاربر می‌بیند در سرویس است (`DependentsMessage`). این دو باید با هم هماهنگ باشند: شرط در repository، پیام در سرویس.

### والد (`RequireParentAsync`)

وقتی کاتالوگ به کاتالوگ دیگری وابسته است (مثلاً «نوع تجهیز» به «دسته‌بندی تجهیز»)، سرویس repository والد را می‌گیرد و در `BeforeCreateAsync` و `BeforeUpdateAsync` از `RequireParentAsync` استفاده می‌کند. نمونه‌ی کامل: `EquipmentTypeService`. قاعده‌ی ویرایش: والد باید فعال باشد اگر **عوض می‌شود** یا رکورد **بازگردانده می‌شود**.

---

## ۵. حذف منطقی به‌صورت فیلتر سراسری

`builder.HasSoftDeleteFilter()` در تنظیمات EF هر کاتالوگ، رکوردهای `IsActive = false` را از **همه‌ی کوئری‌های** آن entity حذف می‌کند، بدون اینکه کسی در هر کوئری `.Where(x => x.IsActive)` بنویسد. برای دیدن رکوردهای غیرفعال: `.IncludingInactive()`.

- `CatalogRepository` در startup بررسی می‌کند که entity فیلتر را دارد (وگرنه برنامه همان‌جا خطا می‌دهد) و تست `Every_catalog_entity_has_the_soft_delete_filter` هم همین را برای همه‌ی `ICatalogEntity`ها تضمین می‌کند.
- **عمداً فقط برای کاتالوگ‌ها اعمال شده، نه همه‌ی entityها.** خطر: entityی که فیلتر ندارد (مثلاً `Equipment`) ولی به رکوردی ارجاع دارد که فیلتر دارد (مثلاً `EquipmentStatus`)، در `Include` ردیف‌هایش را وقتی رکورد ارجاع‌شده غیرفعال شده، بی‌صدا از نتیجه حذف می‌کند. کوئری‌هایی که این `Include`ها را دارند و باید تاریخچه را نشان دهند باید `.IncludingInactive()` بزنند (مثل `EquipmentRepository.GetDetailsAsync`، با تست).
- هشدار EF درباره‌ی همین الگو عمداً خاموش شده، چون یک رکورد فعال هرگز به ردیف غیرفعال یک کاتالوگ ارجاع ندارد (غیرفعال‌سازی با وجود وابسته‌ی فعال رد می‌شود).
- فیلتر هیچ تغییر schema ندارد.

**قاعده‌ی عملی برای تو:** هر وقت کوئری نوشتی که از یک entity دیگر به کاتالوگ می‌رسد (`Include` یا `x.Catalog.Title` داخل `Select`) و باید رکوردهای قدیمی با کاتالوگ غیرفعال را هم نشان بدهد، `.IncludingInactive()` بگذار.

---

## ۶. افزودن یک کاتالوگ جدید (مرجع مراحل)

برای یک مثال کامل با خروجی هر مرحله، [بخش ۷](#۷-مثال-کامل-از-صفر-تا-صد-کاتالوگ-سازنده) را ببین.

### ۱. تولید فایل‌ها با template

یک‌بار نصب (هر توسعه‌دهنده):

```bash
dotnet new install ./templates/catalog
```

سپس **از ریشه‌ی ریپو** (چون مسیر فایل‌ها نسبت به ریشه است) اجرا کن. نام entity به انگلیسی، `Label` نام فارسی برای پیام‌ها:

```bash
dotnet new ems-catalog -n Manufacturer --Label "سازنده"
# دسته‌بندی: جمع بی‌قاعده و بدون کد
dotnet new ems-catalog -n Category --Label "دسته‌بندی" --Plural Categories --HasCode false
```

| گزینه | الزامی؟ | معنی |
|---|---|---|
| `-n` | بله | نام انگلیسی entity (`Manufacturer`). نام کلاس‌ها، فایل‌ها، پوشه‌ها و مسیر API از آن ساخته می‌شود |
| `--Label` | بله | نام فارسی برای پیام‌های خطا (`سازنده`) |
| `--Plural` | خیر | جمع انگلیسی. دو جا استفاده می‌شود: **نام جدول** و **نام گروه permission**. پیش‌فرض: نام + `s` |
| `--HasCode` | خیر (پیش‌فرض `true`) | آیا کاتالوگ فیلد اختیاری `Code` دارد |

#### کی `--Plural` بدهم؟

اگر «نام + `s`» جمع درست انگلیسی نمی‌شود:

| نام | پیش‌فرض | درست |
|---|---|---|
| `Brand` | `Brands` ✓ | |
| `Manufacturer` | `Manufacturers` ✓ | |
| `Category` | `Categorys` ✗ | `--Plural Categories` |
| `Status` | `Statuss` ✗ | `--Plural Statuses` |

اگر ندهی، جدول `Categorys` و permission `Categorys.View` ساخته می‌شود و عوض کردنش بعداً migration و ویرایش می‌خواهد.

#### کی `--HasCode false` بدهم؟

وقتی کاتالوگ فقط عنوان دارد و کد ندارد. در این حالت `Code` از entity، تنظیمات EF، DTOها، فیلتر لیست، مرتب‌سازی و validatorها حذف می‌شود. اگر ندهی، یک فیلد `Code` بی‌مصرف در API و جدول می‌ماند.

### ۲. فایل‌هایی که ساخته می‌شود (۱۵ فایل)

| لایه | فایل‌ها |
|---|---|
| Domain | `Entities/Manufacturer.cs`، `Interfaces/IManufacturerRepository.cs` |
| Infrastructure | `Persistence/Configurations/ManufacturerConfiguration.cs`، `Repositories/ManufacturerRepository.cs` |
| Application | ۵ DTO در `Dtos/Manufacturer/` (Create، Update، Detail، ListItem، ListRequest)، ۳ validator در `Validators/Manufacturer/` (یک‌خطی)، `Interfaces/Manufacturer/IManufacturerService.cs`، `Services/Manufacturer/ManufacturerService.cs` |
| Presentation | `Controllers/ManufacturerController.cs` |

کاری که با آن‌ها نباید بکنی: چیزی در DI یا `EMSDbContext` ثبت کنی (همه خودکار است، [بخش ۲](#چه-چیزی-خودکار-ثبت-میشود)). `DbSet` هم لازم نیست.

تا مرحله‌ی بعد (permission) پروژه build نمی‌شود، چون کنترلر به `Permissions.Manufacturers` ارجاع دارد.

### ۳. مراحل دستی

1. **Permission:** در `EMS.Application/Common/Security/Permissions.cs` یک کلاس `Manufacturers` با چهار ثابت `View/Create/Update/Delete` اضافه کنید (الگوی کلاس‌های موجود، نام کلاس = `Plural`). `Permissions.All` خودکار از روی ثابت‌ها ساخته می‌شود و جای دیگری ثبت لازم نیست. تا این کار را نکنید build نمی‌شود (کنترلر به آن ارجاع دارد).
2. **Migration:**

   ```bash
   dotnet ef migrations add AddManufacturer --project EMS.Infrastructure --startup-project EMS.Presentation
   ```

   یا در Package Manager Console ویژوال استودیو:

   ```
   Add-Migration AddManufacturer -Project EMS.Infrastructure -StartupProject EMS.Presentation
   ```

   اگر فراموش کنید، تست `SchemaDriftTests` شکست می‌خورد (مدل EF را با آخرین migration مقایسه می‌کند). بعد از ساخت، فایل migration را بخوان و مطمئن شو چیزی غیر از تغییرات کاتالوگ جدید در آن نیست. اعمال روی دیتابیس:

   ```bash
   dotnet ef database update --project EMS.Infrastructure --startup-project EMS.Presentation
   ```
   ```
   Update-Database -Project EMS.Infrastructure -StartupProject EMS.Presentation
   ```
3. **وابسته‌ها:** اگر رکورد دیگری به این کاتالوگ ارجاع می‌دهد، `HasActiveDependentsAsync` را در repository پر کنید (پیش‌فرض template فقط `false` برمی‌گرداند) و متن `DependentsMessage` را در سرویس بنویسید. هر دو جا با `TODO` علامت خورده‌اند.
4. **والد (اختیاری):** اگر به کاتالوگ دیگری وابسته است، repository والد را به سازنده‌ی سرویس بدهید و از `RequireParentAsync` در `BeforeCreateAsync` و `BeforeUpdateAsync` استفاده کنید (نمونه: `EquipmentTypeService`). فیلد شناسه‌ی والد را هم به entity، تنظیمات EF (کلید خارجی)، DTOهای Create/Update/Detail/ListItem و validator (`GreaterThan(0)`) اضافه کنید.
5. **فیلتر لیست یا ستون مرتب‌سازی اضافه:** `ApplyFilters` و `ConfigureSorting` را در سرویس بنویسید. فیلد فیلتر را هم به `ListRequestDto` اضافه کنید.
6. **فیلد اضافه** (مثلاً `Description`): در ۶ جا اضافه می‌شود: entity، `Configuration` (طول و نوع ستون)، DTOهای Create/Update/Detail، و در سرویس متدهای `NewEntity`، `ApplyUpdate` و `ToDetail` (و `ListItem` اگر در لیست می‌خواهید). بعد migration.
7. **تست:** یک `CatalogCase` در `EMS.Tests/Integration/CatalogCases.cs` اضافه کنید. بعد همه‌ی تست‌های رفتاری کاتالوگ (حدود ۲۰ سناریو: ایجاد، تکراری، لیست، مرتب‌سازی، ویرایش، rowVersion، حذف و بازگردانی، permission، ...) خودکار روی آن هم اجرا می‌شوند. نمونه‌اش در [بخش ۷](#۷-مثال-کامل-از-صفر-تا-صد-کاتالوگ-سازنده) است.
8. **قرارداد API:** snapshot را عمداً به‌روز کنید:

   ```bash
   UPDATE_API_CONTRACT=1 dotnet test EMS.Tests --filter ApiContractSnapshotTests
   ```

   و تغییر `api-contract.baseline.txt` را در PR بررسی کنید. فقط endpointهای کاتالوگ جدید باید اضافه شده باشند.

### راستی‌آزمایی template
یک entity با کد (`Gadget`) و یکی بدون کد (`Gizmo`) با همین template تولید شد. فقط گروه permission و `CatalogCase` دستی اضافه شد و هر دو کل تست‌های رفتاری کاتالوگ را بدون هیچ کد دستی دیگری گذراندند.

---

## ۷. مثال کامل از صفر تا صد: کاتالوگ «سازنده»

هدف: کاتالوگ `Manufacturer` (سازنده) با عنوان و کد، مثل برند. همه‌ی دستورها از ریشه‌ی ریپو (`EMS/`).

### گام ۱: نصب template (فقط بار اول)

```bash
dotnet new install ./templates/catalog
```

فقط دستور `ems-catalog` را به `dotnet new` اضافه می‌کند و فایلی در پروژه نمی‌سازد.

### گام ۲: تولید

```bash
dotnet new ems-catalog -n Manufacturer --Label "سازنده"
```

`Plural` پیش‌فرض (`Manufacturers`) درست است و کد هم می‌خواهیم، پس گزینه‌ی دیگری نمی‌دهیم. حالا ۱۵ فایل جدول [بخش ۶](#۲-فایلهایی-که-ساخته-میشود-۱۵-فایل) ساخته شده است. چند فایل از آن‌ها را بخوان تا بدانی چه شده:

- `Manufacturer.cs`: `Title` و `Code`، با `ICatalogEntity`.
- `ManufacturerConfiguration.cs`: جدول `Manufacturers`، فیلتر حذف منطقی، طول ستون‌ها از `CatalogConstraints`، ایندکس یکتای `UX_Manufacturers_Title` (فقط رکوردهای فعال).
- `ManufacturerService.cs`: خالی از منطق CRUD. فقط برچسب، پیام وابسته‌ها و نحوه‌ی ساخت و ویرایش و تبدیل به DTO.
- `ManufacturerController.cs`: ۵ action یک‌خطی.

### گام ۳: permission

در `Permissions.cs`، کنار `Brands`:

```csharp
public static class Manufacturers
{
    public const string View = "Manufacturers.View";
    public const string Create = "Manufacturers.Create";
    public const string Update = "Manufacturers.Update";
    public const string Delete = "Manufacturers.Delete";
}
```

### گام ۴: build

```bash
dotnet build
```

باید بدون خطا تمام شود. اگر خطای `Permissions.Manufacturers` گرفتی، گام ۳ را درست انجام نداده‌ای (نام کلاس باید دقیقاً `Plural` باشد).

### گام ۵: migration

```
Add-Migration AddManufacturer -Project EMS.Infrastructure -StartupProject EMS.Presentation
```

در `EMS.Infrastructure/Persistence/Migrations/` دو فایل جدید (`..._AddManufacturer.cs` و `.Designer.cs`) ساخته و `EMSDbContextModelSnapshot.cs` به‌روز می‌شود. فایل اول را باز کن. باید ببینی:

- `CreateTable` برای `Manufacturers` با ستون‌های `ManufacturerId` (کلید)، `Title`، `Code` و ستون‌های مشترک `IsActive`، `CreatedByUserId`، `CreatedDateTime`، `ModifiedByUserId`، `ModifiedDateTime`.
- `CreateIndex` برای `UX_Manufacturers_Title` با `unique: true` و فیلتر `[IsActive] = 1`.

اگر چیز دیگری دیدی (مثلاً تغییر جدول‌های دیگر)، قبل از ادامه دلیلش را پیدا کن.

### گام ۶: اعمال روی دیتابیس

```
Update-Database -Project EMS.Infrastructure -StartupProject EMS.Presentation
```

جدول `Manufacturers` در دیتابیس ساخته می‌شود.

### گام ۷: امتحان در API

برنامه را اجرا کن (`EMS.Presentation` به‌عنوان Startup Project). با کاربر ادمین از `POST /api/Identity/Login` توکن بگیر (ادمین همه‌ی permissionها را دارد، چون `IdentitySeeder` موقع اجرای برنامه permissionهای تازه را به نقش ادمین اضافه می‌کند. اگر ادمین قبلاً توکن داشت، باید دوباره لاگین کند). بعد (ترجیحاً از صفحه‌ی Scalar یا هر کلاینت HTTP):

```bash
# ایجاد (201)
curl -X POST https://localhost:PORT/api/Manufacturer \
  -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"title":"سامسونگ","code":"SMS"}'

# لیست (فقط فعال‌ها)
curl "https://localhost:PORT/api/Manufacturer?pageSize=5&sortBy=title" -H "Authorization: Bearer <token>"

# ایجاد با عنوان تکراری → 409 با code = duplicate_title
# حذف (204)
curl -X DELETE https://localhost:PORT/api/Manufacturer/1 -H "Authorization: Bearer <token>"

# حالا لیست فعال‌ها خالی است، ولی غیرفعال‌ها:
curl "https://localhost:PORT/api/Manufacturer?isActive=false" -H "Authorization: Bearer <token>"
```

(`PORT` را از `launchSettings.json` بردار.)

### گام ۸: تست

۱. در بالای `EMS.Tests/Integration/CatalogCases.cs` using اضافه کن:

```csharp
using EMS.Application.Dtos.Manufacturer;
```

۲. در لیست `All` یک `CatalogCase` اضافه کن (مثل `Brand`):

```csharp
new("Manufacturer", "/api/Manufacturer", "manufacturerId", "Manufacturers", true, typeof(ManufacturerDetailDto), typeof(ManufacturerListItemDto),
    (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title, ["code"] = "MF" })),
```

ترتیب پارامترها: نام، مسیر، نام فیلد شناسه در JSON، نام گروه permission، دارد کد؟، نوع DTO جزئیات، نوع DTO لیست، تابعی که بدنه‌ی ایجاد را می‌سازد (اگر والد دارد، همین‌جا والد را می‌سازی، مثل `EquipmentType`).

۳. اجرا:

```bash
dotnet test EMS.Tests --filter CatalogCrudContractTests
```

حدود ۲۰ سناریو روی سازنده هم اجرا می‌شود. نکته: تست‌ها روی دیتابیس in-memory اجرا می‌شوند (`EmsApiFactory`)، پس برای اجرایشان SQL Server لازم نیست. ایندکس یکتای واقعی فقط روی SQL Server اعمال می‌شود. یکتایی عنوان در برنامه هم با `ExistsByTitleAsync` بررسی می‌شود و تست‌ها همین را می‌سنجند.

### گام ۹: قرارداد API

```bash
UPDATE_API_CONTRACT=1 dotnet test EMS.Tests --filter ApiContractSnapshotTests
```

`EMS.Tests/Contracts/api-contract.baseline.txt` عوض می‌شود. با `git diff` ببین فقط endpointهای `Manufacturer` اضافه شده‌اند.

### گام ۱۰: همه‌ی تست‌ها

```bash
dotnet test
```

همه باید سبز باشد (از جمله `SchemaDriftTests`، `PermissionCatalogTests` و `Every_catalog_entity_has_the_soft_delete_filter`).

### گام ۱۱: commit و PR

ریشه‌ی تغییرات: ۱۵ فایل جدید، `Permissions.cs`، دو فایل migration و snapshot مدل، `CatalogCases.cs` و baseline قرارداد API. در PR دیف baseline را مرور کن.

### بعداً: اگر فیلد دیگری لازم شد (مثال `Country`)

۱. به `Manufacturer.cs` اضافه کن: `public string? Country { get; set; }`.
۲. در `ManufacturerConfiguration`: `builder.Property(x => x.Country).HasMaxLength(60).IsRequired(false);`
۳. در DTOهای Create و Update و Detail (و ListItem اگر لازم است) فیلد را اضافه کن.
۴. در `ManufacturerService`:
   - `NewEntity`: `Country = Normalize(request.Country)`
   - `ApplyUpdate`: `entity.Country = Keep(request.Country, entity.Country);`
   - `ToDetail` (و `ListItem`): `Country = ...`
۵. migration جدید (`Add-Migration AddManufacturerCountry`)، `Update-Database`، تست و baseline.

### بعداً: اگر چیزی به سازنده ارجاع داد

مثلاً مدل تجهیز یک `ManufacturerId` پیدا کرد. دو کار لازم است:
۱. در `ManufacturerRepository.HasActiveDependentsAsync` شرط واقعی بنویس (الگوی بخش ۴).
۲. در `ManufacturerService.DependentsMessage` پیام را اصلاح کن.

---

## ۸. تبدیل یک entity موجود به کاتالوگ (مثال: Skill)

`Skill` الان entity، تنظیمات EF، `DbSet` و جدول دارد (و به `TechnicianSkill` وصل است)، ولی repository و سرویس و کنترلر ندارد. برای اینکه با الگوی کاتالوگ API بگیرد:

### ۱. تولید در پوشه‌ی موقت

اگر مستقیم در ریپو تولید کنی، دو فایل موجود (`Skill.cs` و `SkillConfiguration.cs`) درگیر می‌شوند و ممکن است بازنویسی شوند (و `Description` و `TechnicianSkills` از بین بروند). امن‌تر:

```bash
dotnet new ems-catalog -n Skill --Label "مهارت" -o ../skill-tmp
```

از `../skill-tmp` همه‌ی فایل‌ها را به ریپو کپی کن **به‌جز** `Entities/Skill.cs` و `Configurations/SkillConfiguration.cs`. ۱۳ فایل می‌ماند: repository و interface، ۵ DTO، ۳ validator، سرویس و interface و کنترلر.

### ۲. ادغام دستی دو فایل موجود

`Skill.cs`: فقط `ICatalogEntity` را اضافه کن (بقیه می‌ماند):

```csharp
public class Skill : BaseEntity, IAuditableEntity, ICatalogEntity
```

`SkillConfiguration.cs`: این‌ها را اضافه کن و عددهای ثابت را با ثابت‌های مشترک عوض کن:

```csharp
builder.ToTable("Skills");
builder.HasSoftDeleteFilter();                                                                       // جدید
builder.HasKey(x => x.SkillId);
builder.Property(x => x.Title).HasMaxLength(CatalogConstraints.TitleMaxLength).IsRequired();         // به‌جای 100
builder.Property(x => x.Code).HasMaxLength(CatalogConstraints.CodeMaxLength).IsUnicode(false).IsRequired(false);  // به‌جای 20
builder.Property(x => x.Description).HasMaxLength(500).IsRequired(false);                            // همان قبلی
builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();                                      // جدید
builder.HasIndex(x => x.Title).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_Skills_Title");  // جدید
```

طول‌های 100 و 20 برابر `CatalogConstraints` است، پس migration ستونی را عوض نمی‌کند.

### ۳. فیلدهای اضافه‌ی template

`Description` در template نیست: طبق مرحله‌ی ۶ از [بخش ۶](#۳-مراحل-دستی) در DTOها و سرویس اضافه‌اش کن.

### ۴. وابسته‌ها

```csharp
// SkillRepository
return await _context.TechnicianSkills.AnyAsync(x => x.SkillId == id && x.IsActive, cancellationToken);
```

و `DependentsMessage` را در سرویس اصلاح کن.

### ۵. permission، build

کلاس `Skills` در `Permissions.cs`، بعد `dotnet build`.

### ۶. بررسی داده‌های موجود (فرق مهم با کاتالوگ تازه)

Migration یک ایندکس یکتای عنوان روی رکوردهای فعال می‌سازد. اگر در جدول `Skills` دو رکورد فعال با عنوان یکسان باشد، `Update-Database` شکست می‌خورد. قبل از آن روی دیتابیس بررسی کن:

```sql
SELECT Title, COUNT(*) FROM Skills WHERE IsActive = 1 GROUP BY Title HAVING COUNT(*) > 1;
```

اگر نتیجه داد، تکراری‌ها را اول غیرفعال یا اصلاح کن.

### ۷. migration

```
Add-Migration SkillCatalog -Project EMS.Infrastructure -StartupProject EMS.Presentation
```

فایل را باز کن: باید **فقط** یک `CreateIndex` برای `UX_Skills_Title` باشد. اگر `AlterColumn` یا چیز دیگری دیدی، قبل از `Update-Database` متوقف شو، یعنی چیزی در ادغام مرحله‌ی ۲ نخوانده.

### ۸. تست و قرارداد API
مثل [بخش ۷](#گام-۸-تست) (`CatalogCase` با گروه `Skills` و `HasCode = true`) و baseline.

### ۹. اثر فیلتر حذف منطقی روی کدهای قبلی

با `HasSoftDeleteFilter()` مهارت غیرفعال از کوئری‌هایی که `Skill` را join یا include می‌کنند حذف می‌شود. در `UserManagementService` کوئری مهارت‌های تکنسین `x.Skill.IsActive` را خودش فیلتر می‌کند، پس رفتار تغییری نمی‌کند، ولی مقدار `IsActive = x.Skill.IsActive` در خروجی از این به بعد همیشه `true` است. اگر UI به آن نیاز دارد، بعد از ادغام بررسی کن.

---

## ۹. عیب‌یابی

| نشانه | علت و راه‌حل |
|---|---|
| `Update-Database` / `Add-Migration` در PMC شناخته نمی‌شود | پکیج `Microsoft.EntityFrameworkCore.Tools` باید در پروژه‌ی Startup (`EMS.Presentation`) نصب باشد (هست). بعد از restore، ویژوال استودیو را یک‌بار ببند و باز کن. جایگزین: `dotnet ef ...` |
| build: `'Permissions' does not contain a definition for 'Xxx'` | کلاس permission اضافه نشده یا نامش با `Plural` یکی نیست |
| برنامه بالا نمی‌آید: `... must call builder.HasSoftDeleteFilter()` | در `Configuration` کاتالوگ `builder.HasSoftDeleteFilter()` نیست |
| تست `SchemaDriftTests` شکست می‌خورد | مدل EF تغییر کرده ولی migration نگرفته‌ای (یا snapshot به‌روز نیست) |
| تست `ApiContractSnapshotTests` شکست می‌خورد | قرارداد API عوض شده. اگر عمدی بود با `UPDATE_API_CONTRACT=1` baseline را به‌روز کن و دیف را بررسی کن |
| تست `Every_catalog_entity_has_the_soft_delete_filter` شکست می‌خورد | یک entity با `ICatalogEntity` فیلتر حذف منطقی ندارد |
| `Update-Database` روی `UX_Xxx_Title` خطا می‌دهد | در جدول عنوان تکراری بین رکوردهای فعال هست ([بخش ۸، مرحله ۶](#۶-بررسی-دادههای-موجود-فرق-مهم-با-کاتالوگ-تازه)) |
| `409 has_active_dependents` موقع حذف | رکوردی فعال به این کاتالوگ ارجاع دارد. اول آن را غیرفعال کن یا عوض کن |
| `PUT` روی رکورد غیرفعال، آن را فعال کرد | `isActive` ارسال نشده و پیش‌فرض `true` است ([بخش ۳](#ویرایش-put-apibrand)) |
| در لیست یک entity دیگر رکوردهایی که کاتالوگشان غیرفعال شده ناپدید می‌شوند | کوئری باید `.IncludingInactive()` داشته باشد ([بخش ۵](#۵-حذف-منطقی-به‌صورت-فیلتر-سراسری)) |
| سرویس یا repository تزریق نمی‌شود | نام کلاس به `Service` یا `Repository` ختم نمی‌شود یا interface هم‌نام ندارد |

---

## ۱۰. وقتی کاتالوگ‌مانند نیست

`Equipment`، `FailureReport` و مانند آن‌ها منطق واقعی دارند (وضعیت، گردش کار، ارجاع) و عمداً از این الگو بیرون‌اند. `CatalogService` متدهای `virtual` دارد، ولی اگر یک entity بیشتر از چند hook نیاز دارد، سرویس مستقل بنویسید.

علامت‌های اینکه کاتالوگ نیست:

- عنوان یکتا ندارد، یا یکتایی‌اش شرط دیگری دارد.
- وضعیت یا مرحله دارد (پیش‌نویس، تأییدشده، ...).
- حذفش «غیرفعال‌سازی ساده» نیست (مثلاً باید رکوردهای دیگر هم با آن عوض شوند).
- فیلدهای زیادی دارد که ویرایششان قواعد جداگانه دارد.

در این حالت دستی بنویس: entity از `BaseEntity`، تنظیمات EF، `IXxxRepository` و `XxxRepository` (از `Repository<T>`)، `IXxxService` و `XxxService`، DTOها و validatorها، کنترلر، permission و migration. ثبت در DI و EF همان‌طور که در [بخش ۲](#چه-چیزی-خودکار-ثبت-میشود) آمده خودکار است. الگوی نوشتن را از `EquipmentRepository` و سرویس‌های مربوط به تجهیز بردار.

---

## چک‌لیست افزودن یک کاتالوگ

- [ ] مطمئن شدم کاتالوگ است ([بخش ۱](#۱-آیا-entity-من-کاتالوگ-است))
- [ ] `dotnet new ems-catalog ...` از ریشه‌ی ریپو اجرا شد (۱۵ فایل)
- [ ] کلاس permission با چهار ثابت اضافه شد
- [ ] build موفق است
- [ ] وابسته‌ها (`HasActiveDependentsAsync` و `DependentsMessage`) یا والد پر شد
- [ ] فیلدهای اضافه و فیلتر/مرتب‌سازی اضافه شد (اگر لازم بود)
- [ ] migration ساخته و **خوانده** شد، بعد روی دیتابیس اعمال شد
- [ ] `CatalogCase` اضافه شد
- [ ] baseline قرارداد API به‌روز و دیف مرور شد
- [ ] `dotnet test` سبز است
- [ ] commit و PR
