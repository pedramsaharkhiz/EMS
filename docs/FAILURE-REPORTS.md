# گزارش خرابی

ماژول گزارش خرابی: کاتالوگ‌های پایه، و خود گزارش (`/api/FailureReport`). ارجاع به تکنسین و اقدام‌های انجام‌شده در سند جدا هستند: [TECHNICIAN-ASSIGNMENTS.md](TECHNICIAN-ASSIGNMENTS.md). (جدول `FailureReportActions` حذف شد؛ اقدام‌ها به ارجاع وصل‌اند.)

## کاتالوگ‌ها

همه با الگوی استاندارد [CATALOGS.md](CATALOGS.md): خواندن برای هر کاربر واردشده آزاد است (`[Lookup]`)، نوشتن مجوز `<گروه>.Create/Update/Delete` می‌خواهد، حذف یعنی غیرفعال‌سازی، `rowVersion` و پیام فارسی.

| کاتالوگ | مسیر | مجوزها | نکته |
|---|---|---|---|
| دسته‌ی خرابی | `/api/FailureCategory` | `FailureCategories.*` | از قبل بود. با نوع خرابی فعال غیرفعال نمی‌شود |
| نوع خرابی | `/api/FailureType` | `FailureTypes.*` | `failureCategoryId` الزامی. عنوان فقط **داخل هر دسته** یکتاست. فیلتر لیست با `failureCategoryId` |
| نوع گزارش | `/api/ReportType` | `ReportTypes.*` | ۶ سطر سیستمی (پایین) |
| اقدام | `/api/Action` | `Actions.*` | فیلد اختیاری `description` (حداکثر ۵۰۰ کاراکتر). entity اسمش `ActionEntity` است چون `Action` با `System.Action` تداخل دارد |
| وضعیت گزارش خرابی | `/api/FailureReportStatus` | `FailureReportStatuses.*` | ۳ سطر سیستمی (پایین) |

## سطرهای سیستمی

مثل سمت‌ها ([POSTS.md](POSTS.md)): `id` برابر مقدار enum و `code` برابر نام آن است. فقط **عنوان** قابل ویرایش است. سطر سیستمی غیرفعال یا حذف نمی‌شود (`409 protected_record`)، کدش تغییر نمی‌کند (`protected_record`) و سطر دیگری نمی‌تواند کد سیستمی بگیرد (`409 reserved_code`). سطر دیگر اضافه کردنی است.

| کاتالوگ | enum | سطرها (id: code، عنوان اولیه) |
|---|---|---|
| نوع گزارش | `ReportTypeCode` | 1 Failure خرابی، 2 Incident حادثه، 3 Complaint ثبت شکایت، 4 Request درخواست رسیدگی، 5 MaintenanceNeed نیاز به تعمیر / سرویس، 6 InspectionResult نتیجه‌ی بازرسی |
| وضعیت گزارش خرابی | `FailureReportStatusCode` | 1 Open باز، 2 InProgress در حال رسیدگی، 3 Closed بسته |

**تضمین وجود:** علاوه بر migration، `SystemCatalogsGuard` در هر شروع برنامه سطر گم‌شده را می‌سازد و سطر غیرفعال یا بدکد را برمی‌گرداند. عنوان ویرایش‌شده را دست نمی‌زند.

## گزارش خرابی

| endpoint | مجوز | توضیح |
|---|---|---|
| `GET /api/FailureReport` | `FailureReports.View` | لیست صفحه‌بندی‌شده با فیلتر و مرتب‌سازی |
| `GET /api/FailureReport/{id}` | `FailureReports.View` | جزئیات (فعال یا غیرفعال) |
| `POST /api/FailureReport` | `FailureReports.Create` | ثبت (`201`) |
| `PUT /api/FailureReport/{id}` | `FailureReports.Update` | ویرایش، تغییر وضعیت، بازگردانی |
| `DELETE /api/FailureReport/{id}` | `FailureReports.Delete` | غیرفعال‌سازی (حذف منطقی، تکرارپذیر) |

**فیلدها:** `equipmentId`، `failureTypeId`، `reportTypeId`، `organizationId` (شعبه) الزامی‌اند. `employeeId`، `referenceNumber`، `reportedDateTime` و `description` اختیاری‌اند.

**قواعد:**
- **وضعیت:** ثبت همیشه با «باز» شروع می‌شود (در بدنه‌ی ثبت وضعیتی نیست). وضعیت با `PUT` (`failureReportStatusId`) عوض می‌شود. فعلاً **هیچ قاعده‌ی گذاری** بین وضعیت‌ها نیست.
- **ثبت‌کننده:** `reportedByUserId` از کاربر واردشده پر می‌شود و قابل ویرایش نیست. `employeeId` (کارمند گزارش‌دهنده) جداست و اختیاری.
- **شعبه:** همیشه کاربر می‌دهد و از مکان تجهیز پر نمی‌شود.
- **اولویت:** `priorityId` عدد ۱ تا ۵ است و پاسخ `priorityTitle` هم دارد: ۱ کم، ۲ متوسط، ۳ بالا، ۴ خیلی بالا، ۵ فوری. جدول ندارد (`FailureReportPriorities`).
- **زمان گزارش:** اگر نیاید، زمان فعلی (UTC) می‌شود. زمان آینده (بیش از ۵ دقیقه) رد می‌شود (`400`). در `PUT` اگر نیاید، همان قبلی می‌ماند.
- **شماره‌ی ارجاع:** ورودی دستی و اختیاری، حداکثر ۵۰ کاراکتر. بین گزارش‌های **فعال** یکتاست (بدون توجه به حروف بزرگ و کوچک): `409 duplicate_reference_number`. غیرفعال‌کردن گزارش، شماره را آزاد می‌کند و بازگرداندنش روی شماره‌ی گرفته‌شده `409` می‌دهد.
- **مرجع‌ها:** تجهیز، نوع خرابی، نوع گزارش، وضعیت، شعبه و کارمند باید وجود داشته باشند (`404 parent_not_found`) و در ثبت و در تغییر مرجع فعال باشند (`400 parent_inactive`). گزارشی که مرجع فعلی‌اش بعداً غیرفعال شده، بدون تغییر آن مرجع هنوز قابل ویرایش است و در لیست و جزئیات می‌ماند. بازگردانی گزارش غیرفعال، فعال بودن همه‌ی مرجع‌ها را می‌خواهد.
- **`PUT` همه‌ی فیلدهای قابل ویرایش را جایگزین می‌کند:** `employeeId`، `referenceNumber` و `description` که نیایند پاک می‌شوند. `isActive` اگر نیاید `true` است. `rowVersion` اختیاری است و ناسازگاری `409 concurrency_conflict` می‌دهد.
- هیچ اثری روی وضعیت یا مکان تجهیز ندارد.

**شکل پاسخ (لیست و جزئیات یکسان‌اند):**
- **تجهیز:** `equipmentId`، `equipmentYaasCode`، `equipmentSerialNumber`، `equipmentAssetNumber`، `equipmentOwner` و `equipmentOwnerTitle`، نوع **مؤثر** `equipmentTypeId/Title` (نوع خود تجهیز یا نوع مدلش)، `equipmentModelId/Title` و `equipmentStatusId/Title/Code`. نام‌ها همان نام‌های لیست تجهیزند.
- **کدهای کاتالوگ:** `failureTypeCode` و `reportTypeCode` کنار عنوان‌شان می‌آیند.
- **اشخاص:** کارمند (`employeeName`، `employeePersonnelCode`) و ثبت‌کننده (`reportedBy...` با مجموعه‌ی کامل شخص: نام، کد پرسنلی، وضعیت، سمت، دپارتمان، شهر، استان).

**فیلتر لیست:** `search` (شماره‌ی ارجاع، توضیح، یاس کد تجهیز)، `referenceNumber`، و چندمقداری `equipmentId`، `failureTypeId`، `failureCategoryId`، `reportTypeId`، `statusId`، `priorityId`، `organizationId`، `employeeId`، `reportedByUserId` (مثل `?statusId=1&statusId=2`)، بازه‌ی `reportedFrom` و `reportedTo`، و `isActive`. مرتب‌سازی با `sortBy`: `referenceNumber`، `equipment`، `failureType`، `failureCategory`، `reportType`، `status`، `organization`، `priority`، `reportedDateTime`، `createdDateTime` (پیش‌فرض بر اساس id).

## وابسته‌ها

- نوع خرابی، نوع گزارش و وضعیت وقتی گزارش فعال به آن‌ها ارجاع دارد، و اقدام وقتی اقدامِ ثبت‌شدهٔ یک ارجاع به آن اشاره دارد، غیرفعال نمی‌شوند (`409 has_active_dependents`).

## استقرار

به‌ترتیب `docs/sql/FailureCatalogs.sql` و `docs/sql/FailureReportModel.sql` (یا `dotnet ef database update`). قبل از اجرا روی دیتابیسی که داده دارد، بررسی‌های بالای هر اسکریپت را بزن. گزارش‌های موجود (که API نداشتند) «باز» می‌شوند و به کاربری که ردیف را ساخته نسبت داده می‌شوند.

**ارجاع‌ها:** `GET /api/FailureReport/{id}/assignments` تاریخچهٔ ارجاع‌های گزارش و `POST` همان مسیر ارجاع تازه است؛ جزئیات در [TECHNICIAN-ASSIGNMENTS.md](TECHNICIAN-ASSIGNMENTS.md). برای استقرار، اسکریپت‌های `DropFailureReportActions.sql` و `TechnicianAssignments.sql` را هم به ترتیب اجرا کن.
