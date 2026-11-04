# دپارتمان پشتیبانی و محدوده‌ی شعب

دپارتمان پشتیبانی (`SupportingDepartment`) یک کاتالوگ استاندارد است ([CATALOGS.md](CATALOGS.md)) که به یک شهر وصل است. علاوه بر آن، هر دپارتمان **شعبه‌هایی** دارد که برایشان پشتیبانی می‌کند.

## مدل

- **یک شعبه فقط یک دپارتمان دارد.** رابطه یک‌به‌چند است و ستون روی خود شعبه است: `Organizations.SupportingDepartmentId` (اختیاری). جدول واسط ندارد.
- شعبه‌ای که دپارتمان ندارد، `null` است.
- **هیچ قاعده‌ی جغرافیایی نیست.** شهر یا استان دپارتمان و شعبه لازم نیست یکی باشد (شعبه شهر و استان هم ندارد).

## اختصاص شعب

`POST /api/SupportingDepartment/{id}/organizations` (مجوز `SupportingDepartments.Update`)

```json
{
  "organizationIds": [12, 15],
  "fieldIds": [3],
  "organizationManagementIds": [],
  "zoneIds": [],
  "moveFromOtherDepartments": false
}
```

- هر چهار فهرست اختیاری است ولی حداقل یکی باید مقدار داشته باشد. نتیجه **اجتماع** همه‌ی شعب انتخاب‌شده است (هر شعبه یک بار شمرده می‌شود).
- **گروهی:** `fieldIds` (ناحیه / زمینه‌ی کاری)، `organizationManagementIds` (مدیریت شعب) و `zoneIds` (حوزه) یعنی همه‌ی **شعب فعال** زیرمجموعه، در لحظه‌ی درخواست. شعبه‌ای که بعداً به آن گروه اضافه شود خودکار زیر دپارتمان نمی‌رود. شعبه‌ی زیر مدیریت بدون ناحیه هم با انتخاب مدیریت یا حوزه‌اش می‌آید.
- **همه یا هیچ.** اگر انتخاب مشکلی داشته باشد هیچ تغییری ذخیره نمی‌شود.
- شناسه‌ی ناموجود (هر فهرست)، و شعبه‌ی غیرفعالِ مستقیم‌انتخاب‌شده: `400 bulk_items_invalid` با `error.details[].field` مثل `organizationIds[2]`. شعبه‌های غیرفعال داخل یک گروه ساکت کنار گذاشته می‌شوند.
- دپارتمان ناموجود `404 not_found` و دپارتمان غیرفعال `400 parent_inactive`.
- **تعارض:** اگر شعبه‌ای از قبل زیر دپارتمان **دیگری** است، کل درخواست `409 organizations_already_assigned` می‌دهد و `details` هر شعبه را با نام دپارتمان فعلی‌اش می‌گوید (حداکثر ۵۰). با `moveFromOtherDepartments: true` آن‌ها جابه‌جا می‌شوند.

پاسخ: `{ selected, assigned, moved, unchanged, removed, totalOrganizations }`. `assigned` شعب بدون دپارتمان قبلی، `moved` شعب جابه‌جاشده از دپارتمان دیگر، `unchanged` شعبی که از قبل زیر همین دپارتمان بودند، و `totalOrganizations` تعداد شعب فعال دپارتمان پس از عملیات.

## برداشتن شعب

`DELETE /api/SupportingDepartment/{id}/organizations` با **همان بدنه** (مجوز `SupportingDepartments.Update`؛ `moveFromOtherDepartments` نادیده گرفته می‌شود). فقط شعبی که زیر **همین** دپارتمان‌اند برداشته می‌شوند و بقیه دست‌نخورده می‌مانند (`unchanged`). دپارتمان غیرفعال هم قابل خالی‌شدن است.

## دیدن و فیلتر

- **لیست شعب یک دپارتمان:** `GET /api/OrganizationHierarchy?searchLevel=organization&supportingDepartmentId={id}` (با اجداد هر شعبه: حوزه، مدیریت، ناحیه). endpoint جدا ندارد.
- **شعب بدون دپارتمان:** `...&withoutSupportingDepartment=true`.
- این دو فیلتر فقط برای `searchLevel=organization` معتبرند و هم‌زمان قابل استفاده نیستند. هر ردیف شعبه `supportingDepartmentId` و `supportingDepartmentTitle` دارد.
- لیست و جزئیات دپارتمان `organizationCount` (تعداد شعب فعال) می‌دهد.
- **گزارش خرابی:** `GET /api/FailureReport?supportingDepartmentId=1&supportingDepartmentId=2` گزارش‌هایی را می‌آورد که شعبه‌شان زیر آن دپارتمان‌هاست. هر گزارش `supportingDepartmentId` و `supportingDepartmentTitle` (یا `null`) دارد. چیزی در ثبت یا ویرایش گزارش عوض نشده است.

## غیرفعال‌سازی

دپارتمانی که کاربر، تجهیز **یا شعبه‌ی فعال** دارد غیرفعال نمی‌شود (`409 has_active_dependents`). اول شعب را بردارید.

## استقرار

`docs/sql/OrganizationSupportingDepartment.sql` (یا `dotnet ef database update`). داده تبدیل نمی‌شود: شعب فعلی دپارتمان ندارند تا از API اختصاص داده شوند.
