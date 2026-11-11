# یادداشت‌های ارجاع به تکنسین

تصمیم کسب‌وکاری: بخش ۳.۱۹ `BUSINESS-DECISIONS.md`. تصمیم فنی: [ADR-0025](adr/0025-assignment-notes-and-read-receipts.md).

## موضوعات یادداشت

`NoteCategory` در جدول `NoteCategories` کاتالوگ استاندارد با `CatalogService` و `CatalogRepository` است، بدون کد اضافی. عنوان اجباری، حداکثر ۱۰۰ کاراکتر و میان ردیف‌های فعال یکتا است. حذف منطقی، بازگردانی با PUT، کنترل والد/وابسته و rowVersion مطابق [CATALOGS.md](CATALOGS.md) هستند. موضوع دارای یادداشت فعال قابل غیرفعال‌سازی نیست؛ ابتدا یادداشت‌ها حذف منطقی شوند یا موضوع آن‌ها تغییر کند.

| مسیر | مجوز |
|---|---|
| `GET /api/NoteCategory` و `GET /api/NoteCategory/{id}` | هر کاربر واردشده (`Lookup`) |
| `POST /api/NoteCategory` | `NoteCategories.Create` |
| `PUT /api/NoteCategory/{id}` | `NoteCategories.Update` |
| `DELETE /api/NoteCategory/{id}` | `NoteCategories.Delete` |

## ساختار یادداشت

`AssignmentNotes`: شناسه `AssignmentNoteId`، دو مرجع الزامی `TechnicianAssignmentId` و `NoteCategoryId`، متن `Text` با ستون Unicode و طول محدود **۲۰۰۰ کاراکتر**، و `SeenDateTime` اختیاری. هر ارجاع چند یادداشت دارد. موضوع یادداشت قابل ویرایش است، ولی شناسهٔ ارجاع بعد از ساخت ثابت می‌ماند.

فیلدهای مشترک از `BaseEntity`: `IsActive`، `CreatedByUserId`، `CreatedDateTime`، `ModifiedByUserId` و `ModifiedDateTime`. نویسنده از نشست فعلی سرور تعیین می‌شود و کلاینت نمی‌تواند آن را انتخاب کند. کلیدهای خارجی حذف آبشاری ندارند. تاریخ‌ها UTC با `Z` هستند. متن trim می‌شود؛ فاصلهٔ خالی و متن بیش از ۲۰۰۰ کاراکتر رد می‌شود. قالب JSON، خطاهای فارسی و کدهای پایدار مطابق قرارداد عمومی‌اند.

## دسترسی

مجوز endpoint لازم است و علاوه بر آن قاعدهٔ رکورد در سرویس اجرا می‌شود:

- خواندن: فقط ارجاع‌دهنده (`AssignedByUserId`) و تکنسین (`TechnicianUserId`) همان ارجاع.
- ایجاد: فقط ارجاع‌دهنده.
- ویرایش و حذف: فقط نویسندهٔ همان یادداشت که دسترسی به ارجاع دارد.
- ثبت مشاهده: فقط تکنسین همان ارجاع.
- نقش Admin استثنا ندارد. مجوز به‌تنهایی برای خواندن یادداشت ارجاع دیگران کافی نیست (`403 forbidden`).
- روی ارجاع تکمیل‌شده، ردشده یا لغوشده نیز یادداشت مجاز است. یادداشت وضعیت ارجاع یا گزارش خرابی را تغییر نمی‌دهد.

سیاست قابل جایگزینی `IAssignmentNoteAccessPolicy` در همهٔ مسیرها استفاده می‌شود. محدودیت‌های آینده باید در این سیاست پیاده شوند تا لیست، جزئیات و عملیات نوشتن یکسان بمانند. شناسهٔ ناشناخته `404` و شناسهٔ غیرمثبت `400 validation_failed` است.

## مسیرهای یادداشت

| مسیر | مجوز | رفتار |
|---|---|---|
| `GET /api/TechnicianAssignment/{assignmentId}/notes` | `AssignmentNotes.View` | لیست صفحه‌بندی‌شده برای شرکت‌کنندگان ارجاع |
| `POST /api/TechnicianAssignment/{assignmentId}/notes` | `AssignmentNotes.Create` | ایجاد، `201` و Location |
| `GET /api/AssignmentNote/{id}` | `AssignmentNotes.View` | جزئیات، شامل یادداشت غیرفعال برای شرکت‌کنندگان |
| `PUT /api/AssignmentNote/{id}` | `AssignmentNotes.Update` | ویرایش متن و موضوع توسط نویسنده |
| `DELETE /api/AssignmentNote/{id}?rowVersion=...` | `AssignmentNotes.Delete` | حذف منطقی و تکرارپذیر توسط نویسنده |
| `POST /api/AssignmentNote/{id}/seen` | `AssignmentNotes.MarkSeen` | ثبت مشاهدهٔ صریح تکنسین |

بدنهٔ ایجاد: `{ "noteCategoryId": 1, "text": "متن یادداشت" }`؛ ویرایش همان دو فیلد و `rowVersion` اختیاری. `TechnicianAssignmentId` و نویسنده در بدنه قابل تغییر نیستند. موضوع جدید باید موجود و فعال باشد (`parent_not_found` / `parent_inactive`). حفظ موضوع قبلی بدون تغییر، عنوان و تاریخچهٔ قبلی را از بین نمی‌برد.

لیست: `pageNumber` از ۱، `pageSize` بین ۱ تا ۱۰۰، `noteCategoryId`، `search` در متن، `isSeen` و `isActive` (پیش‌فرض فقط فعال‌ها). مرتب‌سازی با `id` (پیش‌فرض)، `createdDateTime`، `modifiedDateTime` یا `seenDateTime` و جهت `asc` / `desc`؛ شناسه ترتیب را قطعی می‌کند. فیلترها فقط روی یادداشت‌های همان ارجاع اعمال می‌شوند. غیرفعال‌ها با `isActive=false` قابل خواندن‌اند؛ ویرایش یا ثبت مشاهده روی یادداشت غیرفعال `404` است. بازگردانی یادداشت در این نسخه ارائه نشده است.

پاسخ: `id`، `technicianAssignmentId`، `noteCategoryId`، `noteCategoryTitle`، `text`، `seenDateTime`، `isActive`، تاریخ‌ها، `rowVersion` و مجموعهٔ کامل شخص با پیشوند `createdBy` (نام، کد پرسنلی، وضعیت، سمت، دپارتمان، شهر، استان).

## مشاهده و ویرایش هم‌زمان

- `SeenDateTime` هنگام ایجاد **همیشه null** است، حتی اگر ارجاع‌دهنده و تکنسین یک نفر باشند.
- GET لیست یا جزئیات هیچ تغییری در مشاهده نمی‌دهد. رابط کاربری پس از نمایش واقعی یادداشت به تکنسین، endpoint `seen` را صدا می‌زند.
- بدنهٔ seen: `{ "rowVersion": "..." }` یا `{}`. زمان را سرور تعیین می‌کند؛ تاریخ ارسالی کلاینت پذیرفته نمی‌شود.
- اولین مشاهده ثبت می‌شود؛ تکرار درخواست، زمان و نسخه را تغییر نمی‌دهد.
- ویرایش واقعی متن یا موضوع، مشاهده را پاک می‌کند. ذخیرهٔ همان متن و موضوع (پس از trim) بی‌اثر است و زمان مشاهده را نگه می‌دارد.
- rowVersion از `ModifiedDateTime` می‌آید. بهتر است کلاینت در ویرایش، حذف و ثبت مشاهده آن را ارسال کند تا نسخهٔ قدیمی را تأیید یا بازنویسی نکند. تعارض `409 concurrency_conflict` است؛ دوباره بخوانید. دو مشاهدهٔ هم‌زمان نمی‌توانند بی‌صدا زمان یکدیگر را بازنویسی کنند؛ درخواست متعارض را می‌توان پس از خواندن نسخهٔ تازه تکرار کرد.
- تغییر یادداشت و مشاهدهٔ آن، نسخه و تاریخ ویرایش موضوع یا خود ارجاع را تغییر نمی‌دهد.

عملیات ایجاد، ویرایش، حذف و مشاهده با audit استاندارد ثبت می‌شوند؛ متن یادداشت در رویداد مدیریتی فایل لاگ نوشته نمی‌شود. Audit Trail دیتابیس، در صورت فعال‌بودن، مطابق سیاست عمومی مقادیر فیلدهای معمولی را ثبت می‌کند.

## migration و تست

طبق درخواست صاحب محصول، migration و model snapshot در این تغییر ساخته نمی‌شوند. پس از دریافت کد:

```bash
dotnet ef migrations add AddAssignmentNotes --project EMS.Infrastructure --startup-project EMS.Presentation
```

migration تولیدشده باید دو جدول جدید، کلیدهای خارجی و ایندکس‌های آن‌ها را ایجاد کند. آن را بررسی کنید و سپس روی دیتابیس موردنظر اعمال کنید. تا ساخت migration، شکست `SchemaDriftTests` انتظار می‌رود؛ تست حذف یا غیرفعال نشده است.

```bash
dotnet test EMS.Tests --filter "FullyQualifiedName~AssignmentNote|FullyQualifiedName~CatalogCrudContractTests"
```

تست‌های یادداشت روی API واقعی با دیتابیس InMemory، مالکیت، مجوزها، اولین مشاهده، reset پس از تغییر متن/موضوع، درخواست تکراری، تعارض نسخه، حذف منطقی، حالت‌های پایانی ارجاع و فیلترها را می‌سنجند. مدل EF هم روابط، طول ستون، فیلتر حذف منطقی و توکن هم‌زمانی را بررسی می‌کند. این تست‌ها جای اجرای migration روی SQL Server را نمی‌گیرند؛ اجرای تست SQL فعلاً به درخواست صاحب محصول انجام نمی‌شود.
