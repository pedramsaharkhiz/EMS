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

**فیلتر لیست:** `search` (شماره‌ی ارجاع، توضیح، یاس کد تجهیز)، `referenceNumber`، و چندمقداری `equipmentId`، `failureTypeId`، `failureCategoryId`، `reportTypeId`، `statusId`، `priorityId`، `organizationId`، `employeeId`، `reportedByUserId` (مثل `?statusId=1&statusId=2`)، بازه‌ی `reportedFrom` و `reportedTo`، و `isActive` و `hasOpenAssignment` (`true`: دارای ارجاع باز؛ `false`: کاری که هنوز به کسی داده نشده). هر ردیف فیلد `hasOpenAssignment` هم دارد. مرتب‌سازی با `sortBy`: `referenceNumber`، `equipment`، `failureType`، `failureCategory`، `reportType`، `status`، `organization`، `priority`، `reportedDateTime`، `createdDateTime` (پیش‌فرض بر اساس id).

## نوع خرابی و نوع تجهیز (dropdown)

هر نوع خرابی می‌تواند به **چند نوع تجهیز** وصل شود (جدول `FailureTypeEquipmentTypes`)؛ تصمیم‌ها: [BUSINESS-DECISIONS.md ۳.۲۵](BUSINESS-DECISIONS.md)، [ADR-0031](adr/0031-failure-type-applies-to-equipment-types.md).

- **dropdown گزارش:** `GET /api/FailureType?equipmentTypeId={id}` فقط انواع خرابیِ مربوط را می‌آورد: آن‌هایی که به این نوع تجهیز وصل‌اند **و** آن‌هایی که به **هیچ** نوع تجهیزی وصل نیستند (آن‌ها «برای همهٔ انواع» حساب می‌شوند). `id` همان نوع مؤثر تجهیز انتخاب‌شده است (`effectiveTypeId` در پاسخ تجهیز؛ تجهیز یا نوع مستقیم دارد یا از مدلش می‌گیرد). بدون `equipmentTypeId` لیست مثل قبل است. با `failureCategoryId` و `title` ترکیب می‌شود.
- **تعریف رابطه:** `POST /api/FailureType` با `equipmentTypeIds[]`؛ `PUT /api/FailureType/{id}` با `equipmentTypeIds[]` که **کل** فهرست است. نفرستادن فیلد (`null`) فهرست فعلی را **نگه می‌دارد** (کلاینتی که فیلد را نمی‌شناسد چیزی پاک نمی‌کند)؛ فهرست خالی همه را پاک می‌کند و نوع خرابی «برای همه» می‌شود. تکراری یک بار حساب می‌شود. نوع تجهیز ناموجود `404 parent_not_found`، غیرفعال (برای افزودن) `400 parent_inactive`، شناسهٔ نامعتبر `400`.
- **خروجی:** هر آیتم لیست و جزئیات `equipmentTypes[]` دارد (`id`، `title`؛ فقط نوع‌های **فعال**؛ خالی = برای همه).
- **ثبت گزارش اجباری نمی‌کند:** گزارشی که نوع خرابی‌اش به نوع تجهیزش نمی‌خورد رد **نمی‌شود**؛ فقط dropdown فیلتر می‌شود. تغییر رابطه گزارش‌های ثبت‌شده را دست نمی‌زند.
- **استقرار:** جدول تازه است و داده‌ی موجود تغییر نمی‌کند؛ همهٔ انواع خرابی فعلی بی‌رابطه‌اند، پس مثل قبل برای همه نمایش داده می‌شوند. اسکریپت: `docs/sql/FailureTypeEquipmentTypes.sql`.

## داشبورد

سه کارت صفحهٔ اول در [DASHBOARD-DESIGN.md](DASHBOARD-DESIGN.md) تعریف شده‌اند. کارت گزارش خرابی: `GET /api/Dashboard/failure-reports` با مجوز `FailureReports.View`.

## خروجی Excel

`GET /api/FailureReport/export` با **همان** پارامترهای لیست (فیلترها، `sortBy`، `sortDirection`، `isActive`؛ صفحه‌بندی نادیده گرفته می‌شود) و همان مجوز `FailureReports.View`. فایل `گزارش-خرابی-<تاریخ شمسی>.xlsx` است و ستون‌هایش همان چیزهایی‌اند که گرید لیست نشان می‌دهد (عنوان‌ها، نه شناسه‌های فرعی): شناسه، شماره ارجاع، وضعیت، اولویت، نوع گزارش، دسته و نوع خرابی، تجهیز (یاس کد، سریال، شماره دارایی، مالک، نوع، مدل، وضعیت)، شعبه، دپارتمان پشتیبانی، کارمند، ثبت‌کننده (نام، کد پرسنلی، سمت، دپارتمان، شهر، استان)، زمان گزارش، توضیحات، فعال، تاریخ ایجاد. تاریخ‌ها شمسی با ساعت تهران‌اند. سقف و هم‌زمانی مثل بقیهٔ خروجی‌ها ([EXPORTS.md](EXPORTS.md)).

**لیست، جزئیات و Excel یک منبع دارند:** `FailureReportRow`. ثبت‌کننده (و مجموعهٔ شخص او) هم در همان کوئری می‌آید، نه با جست‌وجوی جداگانه، تا فایل و گرید هرگز اختلاف پیدا نکنند. تست‌ها برای هر نوع فیلتر و مرتب‌سازی مقایسه می‌کنند که لیست و فایل دقیقاً همان ردیف‌ها را به همان ترتیب دارند.

**آمادگی برای دسترسی سطح ردیف (row-level security):** در سرویس **یک** نقطهٔ ورود به ردیف‌های لیست و Excel هست، متد خصوصی `Query(request)` در `FailureReportService`. هر قاعده‌ای که تعیین کند «کاربر کدام گزارش‌ها را می‌بیند» باید همان‌جا، یک بار، اعمال شود تا لیست و فایل هر دو محدود شوند. مسیرهای دیگری که یک گزارش را می‌خوانند و باید همان قاعده را بگیرند: جزئیات (`GET /{id}`)، تاریخچهٔ ارجاع‌های گزارش و ارجاع‌های مربوط به گزارش‌ها (ماژول ارجاع). تصمیم: [ADR-0021](adr/0021-list-and-export-share-one-row-source.md).

## وابسته‌ها

- نوع خرابی، نوع گزارش و وضعیت وقتی گزارش فعال به آن‌ها ارجاع دارد، و اقدام وقتی اقدامِ ثبت‌شدهٔ یک ارجاع به آن اشاره دارد، غیرفعال نمی‌شوند (`409 has_active_dependents`).

## استقرار

به‌ترتیب `docs/sql/FailureCatalogs.sql` و `docs/sql/FailureReportModel.sql` (یا `dotnet ef database update`). قبل از اجرا روی دیتابیسی که داده دارد، بررسی‌های بالای هر اسکریپت را بزن. گزارش‌های موجود (که API نداشتند) «باز» می‌شوند و به کاربری که ردیف را ساخته نسبت داده می‌شوند.

**ارجاع‌ها:** `GET /api/FailureReport/{id}/assignments` تاریخچهٔ ارجاع‌های گزارش و `POST` همان مسیر ارجاع تازه است؛ جزئیات در [TECHNICIAN-ASSIGNMENTS.md](TECHNICIAN-ASSIGNMENTS.md). برای استقرار، اسکریپت‌های `DropFailureReportActions.sql` و `TechnicianAssignments.sql` را هم به ترتیب اجرا کن.
