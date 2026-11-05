# تجهیز (Equipment)

تجهیز کاتالوگ نیست (عنوان یکتا ندارد، فیلد و منطق واقعی دارد)، پس سرویس مستقل `EquipmentService` دارد، ولی همان قراردادهای کاتالوگ‌ها را رعایت می‌کند: شناسه‌ی رکورد `id`، `rowVersion` برای ویرایش هم‌زمان، پیام فارسی، کد خطای پایدار، حذف منطقی با `IsActive` و ثبت در audit.

## endpointها

| متد و مسیر | permission | کار |
|---|---|---|
| `GET /api/Equipment` | `Equipments.View` | لیست (فیلتر، مرتب‌سازی، صفحه‌بندی) |
| `GET /api/Equipment/export` | `Equipments.View` | همان لیست، به‌صورت Excel (بدون صفحه‌بندی) |
| `GET /api/Equipment/technicians` | `Equipments.View` | dropdown تکنسین: کاربران **فعال با سمت «تکنسین»** |
| `GET /api/Equipment/{id}/technician` | `Equipments.View` | تکنسین فعلی این تجهیز با سمت و دپارتمان پشتیبانی‌اش؛ `data` برای تجهیز بدون تکنسین `null` است (`404` برای تجهیز ناموجود) |
| `GET /api/Equipment/{id}` | `Equipments.View` | جزئیات (فعال یا غیرفعال) **همراه با پیوست‌های فعال** (`attachments`، جدیدترین اول) و `attachmentCount`؛ [توضیح](ATTACHMENTS.md#پیوست‌ها-در-جزئیات-و-لیست-تجهیز) |
| `GET /api/Equipment/{id}/location-history` | `Equipments.View` | تاریخچه‌ی مکان، جدیدترین اول، صفحه‌بندی‌شده |
| `POST /api/Equipment` | `Equipments.Create` | ایجاد (`201`) |
| `POST /api/Equipment/with-attachments` | `Equipments.Create` | ثبت همراه با فایل‌ها در یک درخواست (`201`)، همه یا هیچ؛ [توضیح](ATTACHMENTS.md#ثبت-تجهیز-همراه-با-پیوست-یک-درخواست) |
| `POST /api/Equipment/bulk` | `Equipments.Create` | ثبت گروهی (`201`)، همه یا هیچ؛ [توضیح](#ثبت-گروهی) |
| `PUT /api/Equipment/{id}` | `Equipments.Update` | ویرایش |
| `POST /api/Equipment/{id}/move` | `Equipments.Assign` | تغییر مکان (با ثبت تاریخچه) |
| `DELETE /api/Equipment/{id}` | `Equipments.Delete` | غیرفعال‌سازی (حذف منطقی، تکرارپذیر) |

## ثبت گروهی

`POST /api/Equipment/bulk` با بدنه‌ی `{ "items": [ ... ] }`. هر عضو آرایه دقیقاً بدنه‌ی ثبت تکی است (کامل و مستقل: نوع یا مدل، وضعیت، موقعیت اولیه و ...) و همان قواعد را دارد.

- **همه یا هیچ.** اگر یک ردیف خطا داشته باشد هیچ تجهیزی ثبت نمی‌شود. یک ذخیره‌ی واحد در یک تراکنش است.
- **سقف ۲۰۰ ردیف** در هر درخواست (`EquipmentConstraints.BulkCreateMaxItems`) و فهرست خالی رد می‌شود.
- **موفق:** `201` و `data` آرایه‌ی تجهیزهای ساخته‌شده (همان `EquipmentDetailDto`) **به ترتیب ورودی**. برای هر تجهیز تاریخچه‌ی «ثبت اولیه تجهیز» و یک رکورد audit نوشته می‌شود.
- **خطا:** همه‌ی مشکل‌ها یک‌جا برمی‌گردند و `error.details[].field` ردیف را نشان می‌دهد، مثل `items[2].yaasCode`.
  - مشکل ورودی (خالی، طول، نوع یا مدل هر دو یا هیچ‌کدام، یاس کد تکراری در خود درخواست): `400 validation_failed`.
  - مشکل داده (یاس کد فعال موجود، مرجع ناموجود یا غیرفعال، تکنسین غیرفعال، قاعده‌ی مکان): `400 bulk_items_invalid`. این‌جا حتی یاس کد تکراری `400` است و نه `409`، چون خطای یک ردیف است.
- مرجع‌ها (وضعیت، نوع، مدل، مکان، سازمان، دپارتمان، تکنسین) به‌ازای هر شناسه‌ی متمایز یک بار بررسی می‌شوند، نه به‌ازای هر ردیف.
- یاس کد تکراری درون درخواست، بدون توجه به حروف بزرگ و کوچک، برای ردیف‌های بعدی خطا می‌دهد.

## قواعد

- **نوع یا مدل:** دقیقاً یکی از `equipmentTypeId` / `equipmentModelId` (قید دیتابیس هم دارد). «نوع مؤثر» (`effectiveTypeId`) یعنی نوع خود تجهیز یا نوعِ مدلش؛ دسته‌بندی از روی آن و برند فقط از مدل می‌آید.
- **`yaasCode` یکتا** بین تجهیزهای **فعال**، بدون حساسیت به حروف و فاصله‌ی اطراف. هم در سرویس (`409 duplicate_yaas_code`) هم با ایندکس یکتای فیلتردار `UX_Equipments_YaasCode` در دیتابیس. تجهیز غیرفعال کدش را آزاد می‌کند؛ بازگرداندنش روی کد گرفته‌شده `409` می‌دهد. `serialNumber` و `assetNumber` یکتا نیستند.
- **ارجاع‌ها** (وضعیت، نوع/مدل، نوع مکان، سازمان، دپارتمان، تکنسین) باید وجود داشته باشند (`404 parent_not_found`) و فعال باشند (`400 parent_inactive`). هنگام ویرایش فقط ارجاع تازه یا بازگرداندن رکورد بررسی می‌شود.
- **`PUT` همه‌ی فیلدهای قابل ویرایش را جایگزین می‌کند:** چیزی که نفرستی (سریال، شماره دارایی، توضیحات) پاک می‌شود. مکان در `PUT` نیست؛ با `move` عوض می‌شود.
- **تغییر مکان:** هر تغییر یک ردیف تاریخچه می‌سازد (کاربر تغییردهنده، زمان، توضیح). ایجاد هم اولین ردیف را می‌سازد. جابه‌جایی به همان مکان فعلی کاری نمی‌کند.
- **حذف منطقی:** تجهیز غیرفعال از لیست پیش‌فرض می‌رود (`?isActive=false` آن را نشان می‌دهد)، `GET` شناسه همچنان کار می‌کند، و بازگرداندنش با `PUT` و `isActive: true` است. فیلتر سراسری حذف منطقی روی `Equipment` فعال است؛ هر کوئریِ جدید که از موجودیت دیگر به `Equipment` وصل می‌شود باید `.IncludingInactive()` داشته باشد (نمونه: `FailureReportRepository.GetDetailsAsync`).

## فیلتر و مرتب‌سازی لیست

همه‌ی فیلترها اختیاری و با AND ترکیب می‌شوند. فیلترهای شناسه چندمقداری‌اند: `?statusId=1&statusId=2`.

- متن: `search` (یاس کد، سریال یا شماره دارایی)، `yaasCode`، `serialNumber`، `assetNumber`.
- شناسه: `statusId`، `categoryId`، `typeId` (نوع مؤثر)، `modelId`، `brandId`، `locationTypeId`، `organizationId`، `fieldId`، `organizationManagementId`، `zoneId`، `supportingDepartmentId`، `cityId`، `provinceId`، `technicianUserId`.
- `createdFrom` / `createdTo` (UTC، ISO 8601، شامل دو سر)، `isActive`.
- `sortBy`: `yaasCode`، `serialNumber`، `assetNumber`، `category`، `type`، `model`، `brand`، `status`، `locationType`، `organization`، `department`، `city`، `province`، `technician`، `createdDateTime` (پیش‌فرض: `id`).

افزودن فیلتر: یک خط در `EquipmentService.Filters` و یک property در `EquipmentListRequestDto`. افزودن ستون: property در `EquipmentRow` (و projection در `EquipmentRepository.ToRows`)، `EquipmentListItemDto`، و یک خط در `EquipmentExport`. جزئیات Excel در [EXPORTS.md](EXPORTS.md).

## مالک تجهیز

- `owner` الزامی است و فقط ۱ (شرکت) یا ۲ (بانک). هر مقدار دیگر (صفر، نامعتبر، یا نبودن) در ثبت، ویرایش، `bulk` و ثبت با فایل `400` فارسی می‌دهد («مالک تجهیز باید شرکت یا بانک باشد.»؛ `null` در JSON پیام عمومی «مقدار این فیلد نامعتبر است.»).
- **جدول هم همین را می‌گوید:** قید `CK_Equipments_Owner` (`[Owner] IN (1, 2)`).
- **تجهیزات موجود:** migration مالک (`AddEquipmentOwner`) ستون را با پیش‌فرض ۰ ساخته بود که مالک نیست. migration `EquipmentOwnerCheck` همه‌ی آن‌ها را **شرکت** می‌کند (تصمیم کسب‌وکار) و بعد قید را می‌گذارد. اسکریپت: `docs/sql/EquipmentOwnerCheck.sql`.

## فیلدهای مشترک پاسخ‌ها

لیست، جزئیات و هر پاسخی که جزئیات تجهیز دارد (ثبت، ویرایش، `move`، `bulk`، ثبت با فایل) **یک شکل** دارند و فایل Excel همان ستون‌ها را نشان می‌دهد:

- **مالک:** `owner` (عدد) و `ownerTitle` (شرکت / بانک). عنوان‌ها فقط در `EquipmentOwnerTitles` نوشته شده‌اند.
- **وضعیت:** `equipmentStatusId`، `equipmentStatusTitle` و `equipmentStatusCode` (نام `EquipmentStatusCode`، مثل `InStock`).
- **جای شعبه در سلسله‌مراتب:** `zoneId/zoneTitle` (حوزه)، `organizationManagementId/Title` (مدیریت شعب)، `fieldId/fieldTitle` (ناحیه). همان سطح‌هایی که لیست روی آن‌ها فیلتر می‌شود.
- **دپارتمان پشتیبانی:** دو چیز متفاوت‌اند و نامشان جداست: `currentSupportingDepartment...` (دپارتمانی که **تجهیز در آن است**) و `currentOrganizationSupportingDepartmentId/Title` (دپارتمانی که **شعبه را پشتیبانی می‌کند**). فیلتر لیست: `supportingDepartmentId` (اولی) و `organizationSupportingDepartmentId` (دومی).
- **تکنسین و محل او:** `technicianUserId`، `technicianName`، `technicianIsActive`، `technicianPostId/Title`، و **محل کار تکنسین**: `technicianSupportingDepartmentId/Title`، `technicianCityId/Title`، `technicianProvinceId/Title` (دپارتمان خودِ کاربر و شهر و استان آن، نه جای تجهیز). برای تجهیز بدون تکنسین همه `null` است. در Excel: «دپارتمان تکنسین»، «شهر تکنسین»، «استان تکنسین».
- **تاریخچه‌ی مکان** (`GET /{id}/location-history`): هر ردیف علاوه بر دپارتمان، `cityId/Title` و `provinceId/Title` همان دپارتمان را دارد، و محل کار **فعلی** تکنسین (`technicianSupportingDepartment...`، `technicianCity...`، `technicianProvince...`). ردیف تاریخچه محل کار تکنسین را در لحظه‌ی خودش ثبت نکرده است، پس محل «فعلی» نشان داده می‌شود.
- **dropdown تکنسین‌ها** و `GET /{id}/technician` هم دپارتمان، شهر و استان او را دارند.
- **نام‌گذاری:** `technicianName` یعنی نامِ تکنسین به‌عنوان **ارجاع از تجهیز** (مثل `reportedByName` در گزارش خرابی)، و `fullName` نامِ خودِ شخص در آیتم‌هایی است که خودِ تکنسین‌اند (dropdown و `/{id}/technician`).

## دسترسی به dropdownهای فرم

`GET /api/Equipment/technicians` و `GET /api/Equipment/owners` مثل بقیه‌ی lookupها (`[Lookup]`) برای **هر کاربر واردشده** باز است. پس کسی که فقط `Equipments.Create` دارد هم می‌تواند فرم ثبت را پر کند. لیست و جزئیات همچنان `Equipments.View` می‌خواهند.

## تکنسین تجهیز

- **فقط کاربر فعالی که سمت «تکنسین» دارد** می‌تواند تکنسین یک تجهیز باشد. این در `GET /technicians`، ثبت (`POST`، `/bulk`، `/with-attachments`) و `move` اعمال می‌شود. کاربر ناموجود `404 parent_not_found`، غیرفعال `400 parent_inactive` و بدون سمت تکنسین `400 technician_post_required` می‌دهد (در ثبت گروهی همان پیام‌ها در `details` ردیف).
- **تکنسین فعلی دست‌نخورده می‌ماند** حتی اگر سمت یا وضعیت آن کاربر بعداً عوض شود. تکنسین فقط با ثبت و `move` عوض می‌شود، پس ویرایش `PUT` به آن کاری ندارد. بازگردانی تجهیز غیرفعال فقط فعال بودن تکنسین قبلی را می‌خواهد، نه سمتش را.
- **`GET /api/Equipment/{id}/technician`:** `id`، `fullName`، `personnelCode`، `isActive`، `postId`، `postTitle`، `supportingDepartmentId` و `supportingDepartmentTitle`. تلفن، ایمیل و کد ملی عمداً نیست. تکنسینِ غیرفعال‌شده یا تجهیز غیرفعال هم نشان داده می‌شود (`isActive: false`). تاریخچه‌ی تکنسین‌های قبلی از `GET /{id}/location-history` می‌آید.

## قاعده‌های باز (منتظر تصمیم کسب‌وکار)

هر کدام در یک نقطه‌ی مشخص‌اند و عوض‌کردنشان به بقیه‌ی کد دست نمی‌زند:

| موضوع | الان | کجا عوض می‌شود |
|---|---|---|
| ترکیب معتبر نوع مکان، سازمان، دپارتمان و تکنسین | هر ترکیبی پذیرفته می‌شود | `IEquipmentLocationRule` (`PermissiveEquipmentLocationRule`) را با قاعده‌ی واقعی جایگزین کن |
| انتقال‌های مجاز وضعیت | همه‌ی انتقال‌ها آزاد است | `EquipmentStatusTransitions.IsAllowed` |
| چه کسی تکنسین می‌شود | **کاربر فعال با سمت «تکنسین»** (`PostCode.Technician`)، در dropdown و در هر ثبت و جابه‌جایی | `EquipmentRepository.QueryTechnicians` و `GetTechnicianEligibilityAsync` (یک نقطه‌ی تصمیم) |
| مانع غیرفعال‌سازی (مثلاً گزارش خرابی باز) | ندارد | `EquipmentService.DeleteAsync` |

وضعیت‌ها همان وضعیت‌های سیستمی `EquipmentStatusCode` هستند (شناسه = مقدار enum، [CATALOGS.md](CATALOGS.md)).

## پیوست‌ها

تجهیز پیوست‌پذیر است (`[Attachable]`): `GET/POST /api/Attachments/Equipments/{id}` و ... با `Equipments.View` و `Equipments.Update`. جزئیات در [ATTACHMENTS.md](ATTACHMENTS.md).

## هنوز در دامنه‌ی این فاز نیست

پر کردن جدول‌های فقط‌خواندنی `Zone`، `OrganizationManagement`، `Field`، `Organization` (از بیرون پر می‌شوند).
