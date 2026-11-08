# ADR-0018: گردش کار ارجاع به تکنسین: ماشین وضعیت، یک ارجاع باز برای هر گزارش، و «فقط خود تکنسین»

- **وضعیت:** پذیرفته‌شده
- **ثبت‌شده در:** ۲۰۲۶-۱۱-۰۸
- **مرتبط:** [ADR-0006](0006-permission-based-authorization-with-live-revalidation.md) (مجوزها)، [ADR-0008](0008-optimistic-concurrency-with-derived-row-version.md) (`rowVersion`)، [ADR-0017](0017-retire-technician-post.md) (هر کاربر موجود می‌تواند تکنسین باشد)

## زمینه
جدول‌های `TechnicianAssignments`، `TechnicianAssignmentStatuses` و `TechnicianAssignmentActions` از قبل بودند ولی API، قاعده و داده نداشتند. enum وضعیت هشت مقدار داشت که دو تای آن («در انتظار»، «ارجاع مجدد») با ساختار «هر ارجاع یک ردیف» جور نبود، جدول وضعیت کد پایدار نداشت، و `EquipmentLocationHistoryId` اقدام الزامی بود (اقدام‌هایی مثل «تعویض قطعه» مکان را عوض نمی‌کنند).

## تصمیم
- **وضعیت‌ها:** شش ردیف سیستمی با `TechnicianAssignmentStatusId == (long)TechnicianAssignmentStatusCode` و `Code` پایدار (مثل وضعیت گزارش خرابی)، با `HasData` در مایگریشن. فقط‌خواندنی از API (`GET /api/TechnicianAssignment/statuses`).
- **ماشین وضعیت در دامنه** (`TechnicianAssignmentCatalog.CanMove`): ارجاع‌شده ← پذیرفته | ردشده | لغو؛ پذیرفته ← در حال انجام | لغو؛ در حال انجام ← تکمیل | لغو. سه وضعیت پایانی خروجی ندارند. سرویس هر تغییر را با همین جدول می‌سنجد (`409 assignment_transition_not_allowed`) و یک تست جدول را کامل پوشش می‌دهد.
- **یک ارجاع باز برای هر گزارش:** ایندکس یکتای فیلترشده `UX_TechnicianAssignments_OpenPerReport` روی `FailureReportId` با شرط `TechnicianAssignmentStatusId IN (1, 2, 4)`. سرویس هم پیش از ذخیره می‌سنجد (پیام روشن `assignment_already_open`)؛ ایندکس رقابت دو درخواست هم‌زمان را می‌بندد. ایندکس غیریکتای قبلی روی `FailureReportId` جایش را به این داد.
- **«فقط خود تکنسین»:** عملیات کار (پذیرش، رد، شروع، تکمیل، ثبت اقدام) علاوه بر مجوز `TechnicianAssignments.Work` در سرویس می‌سنجد که کاربر واردشده همان `TechnicianUserId` است؛ وگرنه `403 forbidden` با استثنای جدید `ForbiddenException`. ارجاع و لغو فقط مجوز `TechnicianAssignments.Assign` می‌خواهند. مدیر سیستم هم استثنا نیست. این اولین قاعدهٔ «مال خودش» پروژه است و مجوزِ به‌تنهایی کافی نیست.
- **ارتباط با گزارش:** شروع کار گزارشِ `Open` را به `InProgress` می‌برد (در همان تراکنش)؛ تکمیل ارجاع گزارش را نمی‌بندد؛ گزارشِ `Closed` ارجاع نمی‌گیرد.
- **ارجاع هرگز حذف یا غیرفعال نمی‌شود؛** لغو راه پس‌گرفتن است و ارجاع پایانی تاریخچه می‌ماند. به همین دلیل DELETE و فیلتر سراسری `IsActive` ندارد.
- **همزمانی:** `ModifiedDateTime` توکن همزمانی است و بدنهٔ هر گذار `rowVersion` اختیاری می‌گیرد (`409 concurrency_conflict` اگر کهنه باشد). حتی بدون آن، دو گذار هم‌زمان روی یک ردیف یکی را رد می‌کنند.
- **اقدام:** فقط `TechnicianAssignmentActions` (جدول گزارش↔اقدام حذف شد: [ADR-0019](0019-drop-failure-report-actions.md)). فقط وقتی ارجاع «در حال انجام» است. `EquipmentLocationHistoryId` **اختیاری** شد؛ اگر داده شود باید به تجهیزِ همان گزارش تعلق داشته باشد.
- **ستون تازه:** `ResultNote` (دلیل رد/لغو یا یادداشت تکمیل). `Description` همان یادداشت ارجاع‌دهنده می‌ماند.
- **مهلت:** اختیاری و آینده. `isOverdue` و فیلتر `overdue` از «مهلت گذشته و هنوز باز» محاسبه می‌شوند، نه ستون ذخیره‌شده.
- **مسیرها:** `POST|GET /api/FailureReport/{id}/assignments`، `GET /api/TechnicianAssignment`، `GET /api/TechnicianAssignment/{id}`، `POST /api/TechnicianAssignment/{id}/{accept|reject|start|complete|cancel|actions}`، `GET .../actions`، `GET /api/Users/{userId}/assignments`، `GET /api/TechnicianAssignment/statuses`.

## گزینه‌های بررسی‌شده
- **هشت وضعیت enum را نگه داریم:** رد شد. «ارجاع مجدد» وضعیتِ یک ارجاع نیست، رویدادی است که ارجاع تازه می‌سازد؛ «در انتظار» با «ارجاع‌شده» تفاوتی در رفتار نداشت.
- **هر کس مجوز دارد برای هر ارجاعی عمل کند:** رد شد. ساده‌تر بود ولی هر کاربر با مجوز کار می‌توانست به‌جای دیگری بپذیرد و تکمیل کند و مسئولیت را بی‌معنا می‌کرد.
- **تکمیل ارجاع گزارش را ببندد:** رد شد. بستن گزارش (مثلاً تأیید شعبه) تصمیم جداست؛ ممکن است تکنسین تمام کند و مشکل برطرف نشده باشد.
- **چند ارجاع باز هم‌زمان برای یک گزارش:** رد شد؛ مسئولیت مبهم می‌شود. اگر بعداً لازم شد، فقط شرط ایندکس و بررسی سرویس برداشته می‌شود.
- **فقط بررسی در سرویس بدون ایندکس:** رد شد؛ رقابت دو درخواست هم‌زمان را نمی‌بندد.

## پیامدها
- مایگریشن `TechnicianAssignments` (و اسکریپت `docs/sql/TechnicianAssignments.sql`) روی دیتابیسی که جدول وضعیت‌ها را پر کرده یا یک گزارش با دو ارجاع دارد شکست می‌خورد؛ سرآیند اسکریپت کوئری بررسی دارد. جدول وضعیت‌ها تاکنون API و seed نداشت، پس باید خالی باشد.
- فرانت باید `403 forbidden` را برای تکنسینِ نادرست نشان دهد و دکمه‌های کار را فقط برای تکنسینِ ارجاع نمایش دهد.
- تغییر عنوان وضعیت‌ها فعلاً از API ممکن نیست (فقط‌خواندنی).
- ارجاع مجدد با یک درخواست (`reassign`)، اعلان مهلت و محدودیت تکنسین به دپارتمان/مهارت اجرا نشده‌اند.
- گزارش بسته ارجاع نمی‌گیرد؛ ولی بسته‌شدن گزارشی که ارجاع باز دارد مانع ندارد (قاعده‌ای خواسته نشده).

## ارجاع
`docs/TECHNICIAN-ASSIGNMENTS.md`، `docs/BUSINESS-DECISIONS.md` (۳.۱۳)، `EMS.Domain/TechnicianAssignmentCatalog.cs`، `EMS.Application/Services/TechnicianAssignment/TechnicianAssignmentService.cs`، `EMS.Infrastructure/Persistence/Migrations/*_TechnicianAssignments.cs`
