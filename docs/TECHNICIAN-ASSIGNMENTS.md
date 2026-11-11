# ارجاع گزارش خرابی به تکنسین

قاعده‌های کسب‌وکاری: [BUSINESS-DECISIONS.md ۳.۱۳](BUSINESS-DECISIONS.md). تصمیم فنی: [ADR-0018](adr/0018-technician-assignment-workflow.md).

## وضعیت‌ها

| شناسه | کد | عنوان | باز؟ |
|---|---|---|---|
| 1 | `Assigned` | ارجاع‌شده | بله |
| 2 | `Accepted` | پذیرفته‌شده | بله |
| 3 | `Rejected` | ردشده | خیر (پایانی) |
| 4 | `InProgress` | در حال انجام | بله |
| 5 | `Completed` | تکمیل‌شده | خیر (پایانی) |
| 6 | `Cancelled` | لغوشده | خیر (پایانی) |

```
Assigned ──accept──> Accepted ──start──> InProgress ──complete──> Completed
    │                    │                    │
    ├──reject──> Rejected│                    │
    └────────────cancel──┴────────cancel──────┘──> Cancelled
```

`GET /api/TechnicianAssignment/statuses` (بدون مجوز خاص، فقط ورود) همین فهرست را با `isOpen` می‌دهد.

## مسیرها

| مسیر | مجوز | شرح |
|---|---|---|
| `POST /api/FailureReport/{id}/assignments` | `TechnicianAssignments.Assign` | ارجاع به تکنسین. بدنه: `technicianUserId`، **`title`** (الزامی، تا ۲۰۰)، **`priorityId`** (الزامی، ۱ تا ۵)، `deadlineDateTime?` (آینده)، `description?`. پاسخ `201` |
| `POST /api/FailureReport/{id}/assignments/bulk` | `TechnicianAssignments.Assign` | **ارجاع گروهی:** چند تکنسین برای یک گزارش. بدنه: `items[]` (۱ تا ۲۰) با `technicianUserId`، **`title`** و **`priorityId`** (هر دو الزامی، مخصوص همان تکنسین)، `description?` و `deadlineDateTime?`. همه یا هیچ. پاسخ `201` با فهرست ارجاع‌ها به ترتیب درخواست |
| `GET /api/FailureReport/{id}/assignments` | `TechnicianAssignments.View` | تاریخچهٔ ارجاع‌های گزارش (فیلتر و مرتب‌سازی مثل لیست) |
| `GET /api/TechnicianAssignment` | `View` | لیست |
| `GET /api/TechnicianAssignment/{id}` | `View` | جزئیات با اقدام‌ها و `rowVersion` |
| `GET /api/Users/{userId}/assignments` | `View` | ارجاع‌های یک کاربر به‌عنوان تکنسین (۴۰۴ برای کاربر ناشناس) |
| `POST /api/TechnicianAssignment/{id}/cancel` | `Assign` | لغو ارجاع باز. `note` اجباری |
| `POST /api/TechnicianAssignment/{id}/seen` | `Work` + خود تکنسین | **ثبت مشاهده:** تکنسین ارجاع را دید (فرانت وقتی صفحهٔ ارجاع واقعاً باز شد صدا می‌زند). فقط **اولین بار** در `viewedDateTime` ثبت می‌شود و تکرار چیزی را عوض نمی‌کند. دیدن پذیرفتن نیست |
| `POST /api/TechnicianAssignment/{id}/accept` | `Work` + خود تکنسین | پذیرش |
| `POST /api/TechnicianAssignment/{id}/reject` | `Work` + خود تکنسین | رد، فقط از «ارجاع‌شده». `note` اجباری |
| `POST /api/TechnicianAssignment/{id}/start` | `Work` + خود تکنسین | شروع؛ گزارشِ «باز» می‌شود «در حال رسیدگی» |
| `POST /api/TechnicianAssignment/{id}/complete` | `Work` + خود تکنسین | تکمیل (`completedDateTime` ثبت می‌شود). گزارش بسته **نمی‌شود** |
| `POST /api/TechnicianAssignment/{id}/actions` | `Work` + خود تکنسین | ثبت اقدام (فقط «در حال انجام»). بدنه: `actionId`، `actionDateTime?` (پیش‌فرض اکنون، نه آینده)، `equipmentLocationHistoryId?`، `description?` |
| `GET /api/TechnicianAssignment/{id}/actions` | `View` | اقدام‌های ارجاع به ترتیب زمان |

بدنهٔ گذارها: `{ "note": "...", "rowVersion": "..." }`، هر دو اختیاری جز `note` در رد و لغو. `rowVersion` کهنه `409 concurrency_conflict` می‌دهد. بدنهٔ خالی `{}` هم پذیرفته است.

## فیلتر لیست

`search` (عنوان، شمارهٔ ارجاع گزارش، یاس کد تجهیز، یادداشت ارجاع‌دهنده)، چندمقداری `failureReportId`، `technicianUserId`، `assignedByUserId`، `statusId`، `priorityId`، و `isSeen` (`false`: تکنسین هنوز ندیده، `true`: دیده)، بازهٔ `assignedFrom`/`assignedTo`، و `overdue` (`true`: باز با مهلت گذشته؛ `false`: بقیه). `sortBy`: `assignedDateTime`، `deadlineDateTime`، `completedDateTime`، `status`، `priority`، `title`، `viewedDateTime`، `reportReference`، `createdDateTime` (پیش‌فرض id).

## شکل پاسخ

شناسه‌ها و وضعیت (`statusId/statusCode/statusTitle`)، **`title`**، **`priorityId`/`priorityTitle`** (۱ کم تا ۵ فوری)، **`viewedDateTime`** و `isSeen`، گزارش و تجهیز (`failureReportId`، `failureReportReferenceNumber`، `equipmentId`، `equipmentYaasCode`)، تکنسین و ارجاع‌دهنده با مجموعهٔ کامل اطلاعات شخص (`technicianName`، `technicianPersonnelCode`، ... و `assignedByName`، ...)، `assignedDateTime`، `deadlineDateTime`، `completedDateTime`، `isOverdue`، `description` (یادداشت ارجاع‌دهنده)، `resultNote` (دلیل رد/لغو یا یادداشت تکمیل)، `actionCount`. جزئیات علاوه بر این `actions[]`، `modifiedDateTime` و `rowVersion` دارد.

## خطاها

| کد | وضعیت | چه وقت |
|---|---|---|
| `assignment_already_open` | 409 | **این تکنسین** برای این گزارش یک ارجاع باز دارد (تکنسین دیگر می‌تواند ارجاع باز داشته باشد) |
| `bulk_items_invalid` | 400 | ارجاع گروهی: بعضی ردیف‌ها رد شدند و چیزی ثبت نشد؛ `details` ردیف و فیلد را نشان می‌دهد (`items[1].technicianUserId`): تکنسین ناشناس یا دارای ارجاع باز روی همین گزارش |
| `failure_report_closed` | 409 | گزارش بسته است |
| `assignment_transition_not_allowed` | 409 | گذار مجاز نیست (مثلاً شروع پیش از پذیرش، یا هر کاری روی ارجاع پایانی) |
| `assignment_not_in_progress` | 409 | ثبت اقدام روی ارجاعی که «در حال انجام» نیست |
| `forbidden` | 403 | کاربر تکنسینِ این ارجاع نیست |
| `user_not_found` | 404 | تکنسین (یا کاربر مسیر) وجود ندارد |
| `parent_not_found` | 404 | اقدام یا ردیف تاریخچهٔ مکان پیدا نشد |
| `validation_failed` | 400 | عنوان یا اولویت نامعتبر، مهلت گذشته، زمان اقدام آینده، دلیل خالی در رد/لغو |

## استقرار

اسکریپت‌های `docs/sql/DropFailureReportActions.sql` و سپس `docs/sql/TechnicianAssignments.sql` (یا `dotnet ef database update`). مجوزهای تازه (`TechnicianAssignments.View/Assign/Work`) برای نقش مدیر سیستم خودکار اضافه می‌شوند؛ برای نقش‌های دیگر از `POST /api/Role/AssignPermissions` استفاده کن.

## یادداشت‌های موضوع‌دار ارجاع

هر ارجاع می‌تواند چند یادداشت مستقل از اقدام‌ها، `Description` و `ResultNote` داشته باشد. فقط ارجاع‌دهنده می‌نویسد؛ ارجاع‌دهنده و تکنسین می‌خوانند؛ فقط نویسنده ویرایش و حذف می‌کند. اولین مشاهدهٔ تکنسین صریحاً ثبت می‌شود و با تغییر واقعی متن یا موضوع پاک می‌شود. وضعیت پایانی ارجاع مانع یادداشت نیست. مسیرها، مجوزها و migration موردنیاز در [ASSIGNMENT-NOTES.md](ASSIGNMENT-NOTES.md) آمده‌اند.

## چند تکنسین برای یک گزارش

یک گزارش می‌تواند هم‌زمان چند ارجاع باز داشته باشد، **یکی برای هر تکنسین** (مثلاً تعمیر برق یخچال و تعمیر درب آن). هر ارجاع مسیر خودش را می‌رود و رد یا لغو یکی بقیه را تغییر نمی‌دهد. شروع اولین ارجاع گزارش را «در حال رسیدگی» می‌کند؛ بستن گزارش همچنان دستی است. کارت «ارجاع‌های باز» در داشبورد **ارجاع‌ها** را می‌شمارد، نه گزارش‌ها را، و «گزارش بدون ارجاع» یعنی گزارش بدون هیچ ارجاع باز. ثبت گروهی هم‌زمان با ارجاع تکی قابل‌استفاده است (می‌شود بعداً تکنسین دیگری اضافه کرد).

برای استقرار اسکریپت `docs/sql/AssignmentOpenPerReportAndTechnician.sql` (ایندکس یکتا عوض می‌شود؛ روی داده‌ی موجود امن است).

## عنوان، اولویت و زمان مشاهده

- **`title`:** عنوان کوتاه ارجاع (مثلاً «تعمیر برق دستگاه») که لیست نشان می‌دهد؛ `description` توضیح کامل و اختیاری است.
- **`priorityId`:** الزامی و **مستقل از اولویت گزارش** (برق یخچال می‌تواند فوری باشد و درب آن عادی). همان مقیاس گزارش خرابی: ۱ کم، ۲ متوسط، ۳ بالا، ۴ خیلی بالا، ۵ فوری (`GET /api/FailureReport/priorities`).
- **`viewedDateTime`:** زمانی که تکنسین **اولین بار** ارجاع را دید؛ خالی یعنی هنوز ندیده. فقط خود تکنسین آن را ثبت می‌کند (`POST .../seen`)، یک‌بار ثبت می‌شود و عوض نمی‌شود. خواندن ارجاع (`GET`) آن را ثبت **نمی‌کند** (بدون اثر جانبی). پذیرش، رد، شروع، تکمیل و ثبت اقدام اگر هنوز خالی باشد آن را هم ثبت می‌کنند (ارجاعی که از لیست پذیرفته شد، دیده‌شده است). لغو توسط ارجاع‌دهنده مشاهده حساب نمی‌شود.
- ویرایش عنوان و اولویت بعد از ثبت ندارد؛ برای اصلاح، ارجاع لغو و دوباره ثبت می‌شود.
- **تغییر شکن:** `title` و `priorityId` الزامی‌اند؛ درخواست‌هایی که آن‌ها را نمی‌فرستند `400` می‌گیرند.
- ارجاع‌های موجود هنگام مایگریشن اولویت گزارششان و عنوان «ارجاع شماره N» را گرفتند و `viewedDateTime` آن‌ها خالی است.
- استقرار: `docs/sql/AssignmentTitlePriorityViewed.sql`.
