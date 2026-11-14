# ADR-0031: نوع خرابی به نوع‌های تجهیز وصل می‌شود؛ بی‌رابطه = برای همه، بدون اجبار

- **وضعیت:** پذیرفته‌شده
- **ثبت‌شده در:** ۲۰۲۶-۱۱-۱۴
- **مرتبط:** [BUSINESS-DECISIONS.md ۳.۲۵](../BUSINESS-DECISIONS.md)، [ADR-0003](0003-shared-catalog-pattern.md)

## زمینه
نوع خرابی (`FailureType`) و نوع تجهیز (`EquipmentType`) هیچ رابطه‌ای نداشتند؛ dropdown گزارش همهٔ انواع خرابی را نشان می‌داد. صاحب محصول خواست با انتخاب تجهیز، فقط انواع خرابیِ همان نوع تجهیز در dropdown بیاید.

## تصمیم
- **جدول واسط** `FailureTypeEquipmentTypes (FailureTypeId, EquipmentTypeId)` با کلید ترکیبی و ایندکس روی `EquipmentTypeId`؛ بدون حذف منطقی (ردیف تا وقتی رابطه هست وجود دارد).
- **بی‌رابطه = برای همه.** فیلتر `?equipmentTypeId=X` می‌گوید `!EquipmentTypes.Any() || EquipmentTypes.Any(l => l.EquipmentTypeId == X)`. پس انواع خرابیِ موجود بدون migration داده به کار خود ادامه می‌دهند.
- **`equipmentTypeIds[]`** در ساخت و ویرایش نوع خرابی. در ویرایش `null` فهرست را **نگه می‌دارد** (مثل `Title` و `Code` در این کاتالوگ) و فهرست خالی پاک می‌کند، تا کلاینت قدیمی ناخواسته رابطه‌ها را پاک نکند. فقط نوع تجهیزِ تازه‌افزوده باید فعال باشد؛ ناموجود `404 parent_not_found`، غیرفعال `400 parent_inactive`. رویداد ممیزی `FailureTypeEquipmentTypesChanged` ثبت می‌شود.
- **خروجی:** `equipmentTypes[]` در لیست و جزئیات، فقط نوع‌های فعال (فیلتر حذف منطقیِ نوع تجهیز از راه ناوبری اعمال می‌شود).
- **اجبار نداریم:** ثبت و ویرایش گزارش درباره‌ی ناسازگاری نوع خرابی و نوع تجهیز بررسی نمی‌کند.

## گزینه‌های بررسی‌شده
- **بی‌رابطه = برای هیچ:** دقیق‌تر ولی بعد از migration همهٔ dropdownها خالی می‌شد تا کسی همه را وصل کند. رد شد.
- **اجبار در ثبت گزارش (`400`):** امن‌تر، ولی گزارش‌های قدیمی و ثبت از ابزارهای دیگر هنگام ویرایش خطا می‌دادند. صاحب محصول فقط فیلتر dropdown را خواست. بعداً اضافه‌شدنی است (یک بررسی در `RequireReferencesAsync`).
- **وصل‌کردن دسته‌بندی خرابی به دسته‌بندی تجهیز:** درشت‌تر و باز هم چند-به-چند. رد شد.
- **فیلتر با `equipmentId` به‌جای `equipmentTypeId`:** سرور باید نوع مؤثر را از تجهیز (و ردیف قابل‌دید RLS) بسازد؛ فرانت `effectiveTypeId` را همین حالا دارد. رد شد.

## پیامدها
- هر نوع خرابیِ تازه یا موجود باید دستی به نوع‌های تجهیز وصل شود تا فیلتر اثر کند؛ تا آن موقع «برای همه» است.
- لیست انواع خرابی `equipmentTypes[]` دارد (حجم پاسخ کمی بیشتر؛ یک زیرکوئری). تست ترجمهٔ SQL Server (`FailureTypeSqlTranslationTests`) آن را می‌پوشاند.
- تغییر `equipmentTypeIds` تنها، `rowVersion` را عوض نمی‌کند (ستون‌های خودِ ردیف نمی‌خورند).

## ارجاع
`EMS.Domain/Entities/FailureTypeEquipmentType.cs`، `EMS.Application/Services/FailureType/FailureTypeService.cs`، `docs/FAILURE-REPORTS.md`، `docs/sql/FailureTypeEquipmentTypes.sql`، `EMS.Tests/Integration/FailureTypeEquipmentTypesTests.cs`
