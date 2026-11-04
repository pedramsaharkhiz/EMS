# گزارش خرابی

این سند مرحله‌به‌مرحله کامل می‌شود. فعلاً **کاتالوگ‌های پایه‌ی** ماژول ساخته شده‌اند. خود گزارش خرابی (جدول `FailureReports`) هنوز API ندارد.

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

## وابسته‌ها

- نوع خرابی، نوع گزارش و اقدام (و وضعیت، پس از اتصال به گزارش) وقتی گزارش یا اقدام فعال به آن‌ها ارجاع دارد غیرفعال نمی‌شوند (`409 has_active_dependents`).

## استقرار

`docs/sql/FailureCatalogs.sql` (یا `dotnet ef database update`). قبل از اجرا روی دیتابیسی که داده دارد، بررسی‌های بالای اسکریپت را بزن.
