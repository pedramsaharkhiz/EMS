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
| `POST /api/FailureReport/{id}/assignments` | `TechnicianAssignments.Assign` | ارجاع به تکنسین. بدنه: `technicianUserId`، `deadlineDateTime?` (آینده)، `description?`. پاسخ `201` |
| `GET /api/FailureReport/{id}/assignments` | `TechnicianAssignments.View` | تاریخچهٔ ارجاع‌های گزارش (فیلتر و مرتب‌سازی مثل لیست) |
| `GET /api/TechnicianAssignment` | `View` | لیست |
| `GET /api/TechnicianAssignment/{id}` | `View` | جزئیات با اقدام‌ها و `rowVersion` |
| `GET /api/Users/{userId}/assignments` | `View` | ارجاع‌های یک کاربر به‌عنوان تکنسین (۴۰۴ برای کاربر ناشناس) |
| `POST /api/TechnicianAssignment/{id}/cancel` | `Assign` | لغو ارجاع باز. `note` اجباری |
| `POST /api/TechnicianAssignment/{id}/accept` | `Work` + خود تکنسین | پذیرش |
| `POST /api/TechnicianAssignment/{id}/reject` | `Work` + خود تکنسین | رد، فقط از «ارجاع‌شده». `note` اجباری |
| `POST /api/TechnicianAssignment/{id}/start` | `Work` + خود تکنسین | شروع؛ گزارشِ «باز» می‌شود «در حال رسیدگی» |
| `POST /api/TechnicianAssignment/{id}/complete` | `Work` + خود تکنسین | تکمیل (`completedDateTime` ثبت می‌شود). گزارش بسته **نمی‌شود** |
| `POST /api/TechnicianAssignment/{id}/actions` | `Work` + خود تکنسین | ثبت اقدام (فقط «در حال انجام»). بدنه: `actionId`، `actionDateTime?` (پیش‌فرض اکنون، نه آینده)، `equipmentLocationHistoryId?`، `description?` |
| `GET /api/TechnicianAssignment/{id}/actions` | `View` | اقدام‌های ارجاع به ترتیب زمان |

بدنهٔ گذارها: `{ "note": "...", "rowVersion": "..." }`، هر دو اختیاری جز `note` در رد و لغو. `rowVersion` کهنه `409 concurrency_conflict` می‌دهد. بدنهٔ خالی `{}` هم پذیرفته است.

## فیلتر لیست

`search` (شمارهٔ ارجاع گزارش، یاس کد تجهیز، یادداشت ارجاع‌دهنده)، چندمقداری `failureReportId`، `technicianUserId`، `assignedByUserId`، `statusId`، بازهٔ `assignedFrom`/`assignedTo`، و `overdue` (`true`: باز با مهلت گذشته؛ `false`: بقیه). `sortBy`: `assignedDateTime`، `deadlineDateTime`، `completedDateTime`، `status`، `reportReference`، `createdDateTime` (پیش‌فرض id).

## شکل پاسخ

شناسه‌ها و وضعیت (`statusId/statusCode/statusTitle`)، گزارش و تجهیز (`failureReportId`، `failureReportReferenceNumber`، `equipmentId`، `equipmentYaasCode`)، تکنسین و ارجاع‌دهنده با مجموعهٔ کامل اطلاعات شخص (`technicianName`، `technicianPersonnelCode`، ... و `assignedByName`، ...)، `assignedDateTime`، `deadlineDateTime`، `completedDateTime`، `isOverdue`، `description` (یادداشت ارجاع‌دهنده)، `resultNote` (دلیل رد/لغو یا یادداشت تکمیل)، `actionCount`. جزئیات علاوه بر این `actions[]`، `modifiedDateTime` و `rowVersion` دارد.

## خطاها

| کد | وضعیت | چه وقت |
|---|---|---|
| `assignment_already_open` | 409 | گزارش یک ارجاع باز دارد |
| `failure_report_closed` | 409 | گزارش بسته است |
| `assignment_transition_not_allowed` | 409 | گذار مجاز نیست (مثلاً شروع پیش از پذیرش، یا هر کاری روی ارجاع پایانی) |
| `assignment_not_in_progress` | 409 | ثبت اقدام روی ارجاعی که «در حال انجام» نیست |
| `forbidden` | 403 | کاربر تکنسینِ این ارجاع نیست |
| `user_not_found` | 404 | تکنسین (یا کاربر مسیر) وجود ندارد |
| `parent_not_found` | 404 | اقدام یا ردیف تاریخچهٔ مکان پیدا نشد |
| `validation_failed` | 400 | مهلت گذشته، زمان اقدام آینده، دلیل خالی در رد/لغو |

## استقرار

اسکریپت‌های `docs/sql/DropFailureReportActions.sql` و سپس `docs/sql/TechnicianAssignments.sql` (یا `dotnet ef database update`). مجوزهای تازه (`TechnicianAssignments.View/Assign/Work`) برای نقش مدیر سیستم خودکار اضافه می‌شوند؛ برای نقش‌های دیگر از `POST /api/Role/AssignPermissions` استفاده کن.

## یادداشت‌های موضوع‌دار ارجاع

هر ارجاع می‌تواند چند یادداشت مستقل از اقدام‌ها، `Description` و `ResultNote` داشته باشد. فقط ارجاع‌دهنده می‌نویسد؛ ارجاع‌دهنده و تکنسین می‌خوانند؛ فقط نویسنده ویرایش و حذف می‌کند. اولین مشاهدهٔ تکنسین صریحاً ثبت می‌شود و با تغییر واقعی متن یا موضوع پاک می‌شود. وضعیت پایانی ارجاع مانع یادداشت نیست. مسیرها، مجوزها و migration موردنیاز در [ASSIGNMENT-NOTES.md](ASSIGNMENT-NOTES.md) آمده‌اند.
