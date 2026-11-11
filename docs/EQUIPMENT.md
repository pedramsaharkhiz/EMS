# تجهیز (Equipment)

تجهیز کاتالوگ نیست (عنوان یکتا ندارد، فیلد و منطق واقعی دارد)، پس سرویس مستقل `EquipmentService` دارد، ولی همان قراردادهای کاتالوگ‌ها را رعایت می‌کند: شناسه‌ی رکورد `id`، `rowVersion` برای ویرایش هم‌زمان، پیام فارسی، کد خطای پایدار، حذف منطقی با `IsActive` و ثبت در audit.

## endpointها

| متد و مسیر | permission | کار |
|---|---|---|
| `GET /api/Equipment` | `Equipments.View` | لیست (فیلتر، مرتب‌سازی، صفحه‌بندی) |
| `GET /api/Equipment/export` | `Equipments.View` | همان لیست، به‌صورت Excel (بدون صفحه‌بندی) |
| `GET /api/Users/{userId}/equipment` | `Equipments.View` | **تجهیزی که یک کاربر الان در اختیار دارد** (او تکنسینشان است)؛ [توضیح](#تجهیز-یک-کاربر) |
| `GET /api/Equipment/technicians` | `Equipments.View` | dropdown تکنسین: **همهٔ کاربران فعال** (سمت معیار نیست) |
| `GET /api/Equipment/{id}/technician` | `Equipments.View` | تکنسین فعلی این تجهیز با سمت و دپارتمان پشتیبانی‌اش؛ `data` برای تجهیز بدون تکنسین `null` است (`404` برای تجهیز ناموجود) |
| `GET /api/Equipment/{id}` | `Equipments.View` | جزئیات (فعال یا غیرفعال) **همراه با پیوست‌های فعال** (`attachments`، جدیدترین اول) و `attachmentCount`؛ [توضیح](ATTACHMENTS.md#پیوست‌ها-در-جزئیات-و-لیست-تجهیز) |
| `GET /api/Equipment/{id}/location-history` | `Equipments.View` | تاریخچه‌ی مکان، جدیدترین اول، صفحه‌بندی‌شده |
| `POST /api/Equipment` | `Equipments.Create` | ایجاد (`201`) |
| `POST /api/Equipment/with-attachments` | `Equipments.Create` | ثبت همراه با فایل‌ها در یک درخواست (`201`)، همه یا هیچ؛ [توضیح](ATTACHMENTS.md#ثبت-تجهیز-همراه-با-پیوست-یک-درخواست) |
| `POST /api/Equipment/bulk` | `Equipments.Create` | ثبت گروهی (`201`)، همه یا هیچ؛ [توضیح](#ثبت-گروهی) |
| `PUT /api/Equipment/{id}` | `Equipments.Update` | ویرایش |
| `POST /api/Equipment/{id}/move` | `Equipments.Assign` | تغییر **جزئی** مکان و/یا تکنسین (با ثبت تاریخچه)؛ [توضیح](#جابه‌جایی-move) |
| `POST /api/Equipment/bulk-move` | `Equipments.Assign` | **همان تغییر `move` برای چند تجهیز** (همه یا هیچ، حداکثر ۲۰۰)؛ [توضیح](#جابه‌جایی-گروهی-bulk-move) |
| `DELETE /api/Equipment/{id}` | `Equipments.Delete` | غیرفعال‌سازی (حذف منطقی، تکرارپذیر) |

## تجهیز یک کاربر

`GET /api/Users/{userId}/equipment` (مجوز `Equipments.View`، مثل لیست تجهیز): تجهیزی که آن کاربر **الان** تکنسینشان است. همان **لیست تجهیز** است با فیلتر `technicianUserId` ثابت‌شده روی کاربرِ مسیر.

- شکل پاسخ، صفحه‌بندی (`pageSize` تا ۱۰۰)، مرتب‌سازی (`sortBy`، `sortDirection`) و همهٔ فیلترهای لیست (`statusId`، `typeId`، `isActive` و ...) همان لیست است. `technicianUserId` اگر در query بیاید **نادیده گرفته می‌شود** (کاربرِ مسیر تعیین‌کننده است).
- تجهیز غیرفعال به‌صورت پیش‌فرض نمی‌آید (`isActive=false` آن را نشان می‌دهد).
- کاربری که چیزی در اختیار ندارد: `200` و لیست خالی. کاربر ناموجود: `404 user_not_found`. کاربر غیرفعال‌شده هم آنچه دارد نشان می‌دهد.
- مجوز `Users.View` لازم **نیست** و کافی هم نیست: خروجی تجهیز است و همان مجوز لیست تجهیز را می‌خواهد (کسی که `Equipments.View` دارد از قبل با `GET /api/Equipment?technicianUserId=…` همین را می‌توانست ببیند، پس دسترسی تازه‌ای باز نمی‌شود).
- نسخهٔ Excel جدا ندارد؛ برای Excel از `GET /api/Equipment/export?technicianUserId=…` استفاده کن.
- **فقط وضعیت فعلی** است، نه تاریخچهٔ آنچه کاربر قبلاً داشته (آن در `GET /api/Equipment/{id}/location-history` هر تجهیز است).

## جابه‌جایی (`move`)

`POST /api/Equipment/{id}/move` (مجوز `Equipments.Assign`) یک تغییر **جزئی** است: **همهٔ فیلدها اختیاری‌اند و `null` (یا نیامدن) یعنی «دست نزن».** دلیل و جزئیات تصمیم: [ADR-0015](adr/0015-partial-equipment-move.md).

```json
{ "currentOrganizationId": 12, "currentSupportingDepartmentId": null, "technicianUserId": 41, "clearTechnician": false, "description": "…", "rowVersion": "…" }
```

| درخواست | نتیجه |
|---|---|
| فقط `technicianUserId` | تکنسین عوض می‌شود، مکان دست‌نخورده |
| فقط `currentOrganizationId` | تجهیز به آن شعبه می‌رود، **ستادِ فعلی خودکار خالی می‌شود**، اگر تکنسین دارد، همراه `clearTechnician: true`؛ وگرنه رد می‌شود |
| فقط `currentSupportingDepartmentId` | به آن ستاد می‌رود، شعبهٔ فعلی خالی می‌شود، اگر تکنسین دارد، همراه `clearTechnician: true`؛ وگرنه رد می‌شود |
| تکنسین + ستاد | هر دو در یک ردیف تاریخچه عوض می‌شوند؛ تکنسین همراه شعبه ممنوع است |
| شعبه و ستاد هم‌زمان | `400 validation_failed` (تجهیز دقیقاً یکی را دارد) |
| `clearTechnician: true` | تکنسین برداشته می‌شود. با `technicianUserId` هم‌زمان نمی‌آید (`400`) |
| هیچ‌کدام از چهار مورد بالا | `400 validation_failed` («حداقل یکی را بفرست») |
| همان وضعیت فعلی | `200` بدون خطا و بدون ردیف تاریخچه |

- **`null` دیگر تکنسین را پاک نمی‌کند** (قبلاً می‌کرد). برای برداشتن تکنسین `clearTechnician: true` بفرست.
- **تکنسین هنگام جابه‌جایی بین ستادها می‌ماند** مگر اینکه تکنسین تازه بیاید یا `clearTechnician` باشد. انتقال به شعبه با تکنسین ممنوع است و باید صریحاً `clearTechnician: true` فرستاد.
- **ردیف تاریخچه عکس کامل وضعیت بعد از تغییر است** (مکان و تکنسین)، حتی وقتی فقط تکنسین عوض شده.
- **فقط وجود بررسی می‌شود:** شعبه، ستاد و تکنسینِ ناموجود `404 parent_not_found`. **فعال‌بودن شعبه و ستاد در `move` بررسی نمی‌شود. سمت و فعال‌بودن تکنسین در هیچ‌جا بررسی نمی‌شود** (بخش «تکنسین تجهیز»).
- تجهیز غیرفعال را هم می‌شود جابه‌جا کرد (`404 not_found` فقط برای تجهیز ناموجود). `rowVersion` فقط وقتی تغییری هست بررسی می‌شود (`409 concurrency_conflict`).

## جابه‌جایی گروهی (`bulk-move`)

`POST /api/Equipment/bulk-move` (مجوز `Equipments.Assign`): **یک** تغییر برای **چند** تجهیز. مقصد برای همه یکسان است؛ فیلدها و معنایشان دقیقاً همان `move` تکی است. دلیل و جزئیات: [ADR-0016](adr/0016-bulk-equipment-move.md).

```json
{ "equipmentIds": [1, 2, 3], "currentOrganizationId": 12, "technicianUserId": null, "clearTechnician": false, "description": "…" }
```

- **`equipmentIds`:** ۱ تا ۲۰۰ شناسه، بدون تکرار و مثبت (وگرنه `400 validation_failed`). تجهیز غیرفعال هم جابه‌جا می‌شود.
- **تغییر:** مثل `move` تکی. نیامدن یا `null` یعنی دست نزن؛ شعبه، ستاد را خالی می‌کند و برعکس؛ شعبه و ستاد هم‌زمان `400`؛ `clearTechnician`؛ درخواستی که هیچ تغییری نخواهد `400`. **مقصد برای همه یکسان است**، پس وقتی همه به یک شعبه می‌روند، تکنسین باید با `clearTechnician: true` برداشته شود وگرنه کل درخواست رد می‌شود، و وقتی همه به یک تکنسین می‌روند، مکان هر تجهیز می‌ماند.
- **همه یا هیچ:** یک تراکنش. شناسهٔ ناموجود: `400 bulk_items_invalid` با `error.details[].field` مثل `equipmentIds[1]` و بدون هیچ تغییری. شعبه، ستاد یا تکنسین ناموجود: `404 parent_not_found`. فقط **وجود** مقصد بررسی می‌شود (مثل `move`).
- **تاریخچه:** برای هر تجهیز تغییرکرده یک ردیف (عکس کامل وضعیت) با همان `description` برای همه. تجهیزی که از قبل در آن وضعیت بود ردیف نمی‌گیرد.
- **پاسخ (`200`):** `{ "selected": 3, "changed": 2, "unchanged": 1, "changedIds": [1, 3] }`. اگر هیچ‌کدام تغییر نکند هم موفق است (`changed: 0`).
- **هم‌زمانی:** `rowVersion` ندارد؛ اگر تجهیزی بین خواندن و ذخیره توسط دیگری عوض شود، کل درخواست `409 concurrency_conflict` می‌گیرد.

## ثبت گروهی

`POST /api/Equipment/bulk` با بدنه‌ی `{ "items": [ ... ] }`. هر عضو آرایه دقیقاً بدنه‌ی ثبت تکی است (کامل و مستقل: نوع یا مدل، وضعیت، موقعیت اولیه و ...) و همان قواعد را دارد.

- **همه یا هیچ.** اگر یک ردیف خطا داشته باشد هیچ تجهیزی ثبت نمی‌شود. یک ذخیره‌ی واحد در یک تراکنش است.
- **سقف ۲۰۰ ردیف** در هر درخواست (`EquipmentConstraints.BulkCreateMaxItems`) و فهرست خالی رد می‌شود.
- **موفق:** `201` و `data` آرایه‌ی تجهیزهای ساخته‌شده (همان `EquipmentDetailDto`) **به ترتیب ورودی**. برای هر تجهیز تاریخچه‌ی «ثبت اولیه تجهیز» و یک رکورد audit نوشته می‌شود.
- **خطا:** همه‌ی مشکل‌ها یک‌جا برمی‌گردند و `error.details[].field` ردیف را نشان می‌دهد، مثل `items[2].yaasCode`.
  - مشکل ورودی (خالی، طول، نوع یا مدل هر دو یا هیچ‌کدام، سریال یا شماره دارایی تکراری در خود درخواست): `400 validation_failed`.
  - مشکل داده (سریال یا شماره دارایی که تجهیز دیگری دارد، مرجع ناموجود یا غیرفعال (غیر از تکنسین)، قاعده‌ی مکان): `400 bulk_items_invalid`. این‌جا حتی سریال تکراری `400` است و نه `409`، چون خطای یک ردیف است.
- مرجع‌ها (وضعیت، نوع، مدل، مکان، شعبه، دپارتمان، تکنسین) به‌ازای هر شناسه‌ی متمایز یک بار بررسی می‌شوند، نه به‌ازای هر ردیف.
- سریال یا شماره دارایی تکراری درون درخواست (بدون توجه به حروف بزرگ و کوچک و فاصله‌ی اطراف) برای ردیف‌های بعدی خطا می‌دهد. یاس کد می‌تواند تکرار شود.

## قواعد

- **نوع یا مدل:** دقیقاً یکی از `equipmentTypeId` / `equipmentModelId` (قید دیتابیس هم دارد). «نوع مؤثر» (`effectiveTypeId`) یعنی نوع خود تجهیز یا نوعِ مدلش؛ دسته‌بندی از روی آن و برند فقط از مدل می‌آید.
- **یکتایی:** `yaasCode` **یکتا نیست** (چند تجهیز می‌توانند یاس کد یکسان داشته باشند). **`serialNumber` و `assetNumber` هر کدام یکتایند، سراسری بین همه‌ی تجهیزات** (فعال و غیرفعال)، بدون حساسیت به حروف و فاصله‌ی اطراف: `409 duplicate_serial_number` و `409 duplicate_asset_number`. تجهیز بدون سریال (یا بدون شماره دارایی) با هیچ‌کدام برخورد نمی‌کند و مقدار خالی «هیچ» است. تجهیز غیرفعال شماره‌هایش را نگه می‌دارد (همان دستگاه فیزیکی است)، پس بازگرداندنش مشکلی ندارد و تجهیز تازه نمی‌تواند همان شماره را بگیرد. هم در سرویس بررسی می‌شود و هم با ایندکس‌های یکتای `UX_Equipments_SerialNumber` و `UX_Equipments_AssetNumber`؛ اگر دو درخواست هم‌زمان از سرویس رد شوند، ایندکس همان پیام و کد فارسی را می‌دهد (`UniqueConstraintMessages`).
- **کارایی بررسی یکتایی:** در SQL Server بررسی سریال و شماره دارایی ستون را مستقیم با `=` مقایسه می‌کند و به **collation حساس‌نبودن به حروف** (`_CI_`) ستون تکیه دارد؛ پس از ایندکس یکتا استفاده می‌شود (seek، نه خواندن کل جدول). این collation باید روی دیتابیس واقعی تأیید شود (کوئری بررسی در سرآیند `docs/sql/RefreshTokenIndexes.sql` است). تست‌ها با دیتابیس حافظه کار می‌کنند که حساس به حروف است، پس همان‌جا ستون `ToLower` می‌شود تا رفتار یکی بماند.
- **محل تجهیز: شعبه یا ستاد، دقیقاً یکی.** هر تجهیز **یا** `currentOrganizationId` (شعبه) دارد **یا** `currentSupportingDepartmentId` (دپارتمان پشتیبانی، «ستاد»)، نه هر دو و نه هیچ‌کدام. در دیتابیس قید `CK_Equipments_Location_ExactlyOne` (و همتایش روی تاریخچه، `CK_EquipmentLocationHistories_Location_ExactlyOne`) است و در API هم اعتبارسنجی فارسی دارد (`400 validation_failed` روی `currentOrganizationId`): ثبت، ثبت گروهی، ثبت با فایل و `move`. کاتالوگ «نوع مکان» **حذف شده** ([ADR-0014](adr/0014-equipment-is-with-a-branch-or-a-supporting-department.md)). پاسخ‌ها `locationKind` (۱ = شعبه، ۲ = ستاد) و `locationKindTitle` («شعبه» / «ستاد») را از روی همان دو ستون می‌دهند. با `move` تجهیز از شعبه به ستاد (یا برعکس) می‌رود و مقدار قبلی `null` می‌شود. تجهیزِ ستادی `currentCity/Province` دارد (از دپارتمان) و حوزه و ناحیه ندارد؛ تجهیزِ شعبه‌ای برعکس.
- **ارجاع‌ها** (وضعیت، نوع/مدل، شعبه، دپارتمان) باید وجود داشته باشند (`404 parent_not_found`) و فعال باشند (`400 parent_inactive`). **تکنسین** فقط باید **وجود** داشته باشد (`404 parent_not_found`): نه فعال‌بودنش و نه سمتش بررسی می‌شود. هنگام ویرایش فقط ارجاع تازه یا بازگرداندن رکورد بررسی می‌شود.
- **`PUT` همه‌ی فیلدهای قابل ویرایش را جایگزین می‌کند:** چیزی که نفرستی (سریال، شماره دارایی، توضیحات) پاک می‌شود. مکان در `PUT` نیست؛ با `move` عوض می‌شود.
- **تغییر مکان:** هر تغییر یک ردیف تاریخچه می‌سازد (کاربر تغییردهنده، زمان، توضیح). ایجاد هم اولین ردیف را می‌سازد. جابه‌جایی به همان مکان فعلی کاری نمی‌کند.
- **حذف منطقی:** تجهیز غیرفعال از لیست پیش‌فرض می‌رود (`?isActive=false` آن را نشان می‌دهد)، `GET` شناسه همچنان کار می‌کند، و بازگرداندنش با `PUT` و `isActive: true` است. فیلتر سراسری حذف منطقی روی `Equipment` فعال است؛ هر کوئریِ جدید که از موجودیت دیگر به `Equipment` وصل می‌شود باید `.IncludingInactive()` داشته باشد (نمونه: `FailureReportRepository.GetDetailsAsync`).

## فیلتر و مرتب‌سازی لیست

همه‌ی فیلترها اختیاری و با AND ترکیب می‌شوند. فیلترهای شناسه چندمقداری‌اند: `?statusId=1&statusId=2`.

- متن: `search` (یاس کد، سریال یا شماره دارایی)، `yaasCode`، `serialNumber`، `assetNumber`.
- شناسه: `statusId`، `categoryId`، `typeId` (نوع مؤثر)، `modelId`، `brandId`، `locationKind` (۱ = با شعبه، ۲ = با ستاد)، `organizationId`، `fieldId`، `organizationManagementId`، `zoneId`، `supportingDepartmentId` (دپارتمانی که شعبه را پشتیبانی می‌کند)، `currentSupportingDepartmentId` (دپارتمانی که خودِ تجهیز در آن است)، `cityId`، `provinceId`، `technicianUserId`.
- `createdFrom` / `createdTo` (UTC، ISO 8601، شامل دو سر)، `isActive`.
- `sortBy`: `yaasCode`، `serialNumber`، `assetNumber`، `category`، `type`، `model`، `brand`، `status`، `locationKind`، `organization`، `department`، `city`، `province`، `technician`، `createdDateTime` (پیش‌فرض: `id`).

افزودن فیلتر: یک خط در `EquipmentService.Filters` و یک property در `EquipmentListRequestDto`. افزودن ستون: property در `EquipmentRow` (و projection در `EquipmentRepository.ToRows`)، `EquipmentListItemDto`، و یک خط در `EquipmentExport`. جزئیات Excel در [EXPORTS.md](EXPORTS.md).

## نوع و مدل در فرم تجهیز

تجهیز یا **نوع** دارد یا **مدل** (قید دیتابیس): تجهیزی که مدل ندارد فقط نوعش پر می‌شود و تجهیزی که مدل دارد فقط مدلش (نوعش از روی مدل می‌آید: `effectiveTypeId`). دو lookup برای فرم (هر کاربر واردشده؛ مثل بقیه‌ی کاتالوگ‌ها):

- **`GET /api/EquipmentType/without-models`** نوع‌های **فعال** که **هیچ مدل فعالی ندارند** (تجهیز چنین نوعی را فقط با خود نوع می‌شود ثبت کرد)، همراه با دسته‌شان: `id`، `title`، `code`، `isActive`، `equipmentCategoryId`، `equipmentCategoryTitle`. صفحه‌بندی‌شده؛ فیلتر `title` (شامل) و `equipmentCategoryId`؛ `sortBy`: `title` (پیش‌فرض)، `code`، `category`. وقتی تنها مدل یک نوع غیرفعال شود، آن نوع دوباره در این فهرست می‌آید.
- **`GET /api/EquipmentType/by-model/{modelId}`** نوعِ آن مدل با دسته‌اش، وقتی کاربر مدل را انتخاب می‌کند (برای پر کردن خودکار نوع و دسته). مدل یا نوع غیرفعال هم جواب می‌دهد (با `isActive: false`)؛ مدل ناموجود `404`.
- **`hasModels`:** هر ردیف نوع (در `GET /api/EquipmentType`، `without-models` و `by-model`) فیلد بولی `hasModels` دارد: `true` یعنی نوع حداقل یک مدل **فعال** دارد (همان معنای `without-models`، که فقط ردیف‌های `false` را می‌آورد). مدل غیرفعال حساب نمی‌شود. در لیست نوع‌های غیرفعال (`isActive=false`) هم همین قاعده است. فرانت از آن می‌فهمد برای این نوع باید مدل انتخاب شود یا خود نوع مستقیم ثبت می‌شود.
- **شکل یکسان:** این دو مسیر همان شکل آیتم لیست «نوع تجهیز» را برمی‌گردانند (`EquipmentTypeListItemDto`)؛ دسته‌ی نوع همیشه با `equipmentCategoryId/Title` می‌آید، در لیست و جزئیات و پاسخ ثبت هم.
- **عنوان والد در کاتالوگ‌ها:** مدل تجهیز `equipmentTypeTitle` و `brandTitle`، نوع تجهیز `equipmentCategoryTitle` و نوع خرابی `failureCategoryTitle` را کنار شناسه‌ی والد دارند (مثل شهر که `provinceTitle` دارد).
- **قاعده‌ای اضافه نشد:** نوعی که مدل دارد را هم هنوز می‌شود مستقیم به تجهیز داد؛ این فهرست فقط «نوع‌هایی که چاره‌ای جز ثبت مستقیم ندارند» را نشان می‌دهد.

## اعتبارسنجی در برابر قیدهای دیتابیس

برای هر قید جدول تجهیز، اعتبارسنجی فارسی هم هست تا درخواست پیش از رسیدن به دیتابیس رد شود (ثبت، ویرایش، `bulk` و ثبت با فایل): طول `yaasCode`، `serialNumber`، `assetNumber` و `description` (۵۰ / ۵۰ / ۵۰ / ۵۰۰)، ستون‌های الزامی (`yaasCode`، `equipmentStatusId`، `owner`)، سه قید `CHECK` (دقیقاً یکی از نوع یا مدل؛ مالک فقط ۱ یا ۲؛ دقیقاً یکی از شعبه یا دپارتمان)، کلیدهای خارجی (۷ ارجاع، `404 parent_not_found`) و ایندکس‌های یکتا. تست `EquipmentConstraintCoverageTests` فهرست قیدها را از مدل EF می‌خواند و **دقیق** مقایسه می‌کند: قیدی که بدون اعتبارسنجی اضافه شود، تست را می‌شکند.

## مالک تجهیز

- `owner` الزامی است و فقط ۱ (شرکت) یا ۲ (بانک). هر مقدار دیگر (صفر، نامعتبر، یا نبودن) در ثبت، ویرایش، `bulk` و ثبت با فایل `400` فارسی می‌دهد («مالک تجهیز باید شرکت یا بانک باشد.»؛ `null` در JSON پیام عمومی «مقدار این فیلد نامعتبر است.»).
- **جدول هم همین را می‌گوید:** قید `CK_Equipments_Owner` (`[Owner] IN (1, 2)`).
- **تجهیزات موجود:** migration مالک (`AddEquipmentOwner`) ستون را با پیش‌فرض ۰ ساخته بود که مالک نیست. migration `EquipmentOwnerCheck` همه‌ی آن‌ها را **شرکت** می‌کند (تصمیم کسب‌وکار) و بعد قید را می‌گذارد. اسکریپت: `docs/sql/EquipmentOwnerCheck.sql`.

## فیلدهای مشترک پاسخ‌ها

لیست، جزئیات و هر پاسخی که جزئیات تجهیز دارد (ثبت، ویرایش، `move`، `bulk`، ثبت با فایل) **یک شکل** دارند و فایل Excel همان ستون‌ها را نشان می‌دهد:

- **مالک:** `owner` (عدد) و `ownerTitle` (شرکت / بانک). عنوان‌ها فقط در `EquipmentOwnerTitles` نوشته شده‌اند.
- **وضعیت:** `equipmentStatusId`، `equipmentStatusTitle` و `equipmentStatusCode` (نام `EquipmentStatusCode`، مثل `InStock`).
- **جای شعبه در سلسله‌مراتب:** `zoneId/zoneTitle` (ناحیه)، `organizationManagementId/Title` (مدیریت شعب)، `fieldId/fieldTitle` (حوزه). همان سطح‌هایی که لیست روی آن‌ها فیلتر می‌شود.
- **دپارتمان پشتیبانی:** دو چیز متفاوت‌اند و نامشان جداست: `supportingDepartmentId/Title` (دپارتمانی که **شعبه را پشتیبانی می‌کند**؛ همان نامی که در گزارش خرابی و سلسله‌مراتب هم هست) و `currentSupportingDepartmentId/Title` (دپارتمانی که **خودِ تجهیز در آن است**). فیلتر لیست هم با همین نام‌هاست: `supportingDepartmentId` و `currentSupportingDepartmentId`. **توجه:** `supportingDepartmentId` پیش‌تر دپارتمانِ خودِ تجهیز را فیلتر می‌کرد و حالا دپارتمان شعبه را؛ فیلتر دپارتمانِ خودِ تجهیز `currentSupportingDepartmentId` است.
- **شهر و استان تجهیز:** `currentCityId/Title` و `currentProvinceId/Title` (شهر و استانِ دپارتمانی که تجهیز در آن است). فیلترهای `cityId` و `provinceId` بدون تغییر ماندند.
- **تکنسین و محل او:** `technicianUserId`، `technicianName`، `technicianIsActive`، `technicianPostId/Title`، و **محل کار تکنسین**: `technicianSupportingDepartmentId/Title`، `technicianCityId/Title`، `technicianProvinceId/Title` (دپارتمان خودِ کاربر و شهر و استان آن، نه جای تجهیز). برای تجهیز بدون تکنسین همه `null` است. در Excel: «دپارتمان تکنسین»، «شهر تکنسین»، «استان تکنسین».
- **تاریخچه‌ی مکان** (`GET /{id}/location-history`): مکانِ **در لحظه‌ی همان ثبت** با همان نام‌های لیست می‌آید: `locationKind` و `locationKindTitle`، `currentOrganizationId/Title`، `currentSupportingDepartmentId/Title`، `currentCityId/Title`، `currentProvinceId/Title`. تکنسین و ثبت‌کننده‌ی تغییر (`changedBy...`) هر دو مجموعه‌ی کامل شخص را دارند (نام، کد پرسنلی، وضعیت، سمت، دپارتمان، شهر، استان). محل کار تکنسین همان محل **فعلی** اوست؛ ردیف تاریخچه آن را در لحظه‌ی خودش ثبت نکرده است.
- **dropdown تکنسین‌ها** و `GET /{id}/technician` هم دپارتمان، شهر و استان او را دارند.
- **نام‌گذاری:** `technicianName` یعنی نامِ تکنسین به‌عنوان **ارجاع از تجهیز** (مثل `reportedByName` در گزارش خرابی)، و `fullName` نامِ خودِ شخص در آیتم‌هایی است که خودِ تکنسین‌اند (dropdown و `/{id}/technician`).

## دسترسی به dropdownهای فرم

`GET /api/Equipment/technicians` و `GET /api/Equipment/owners` مثل بقیه‌ی lookupها (`[Lookup]`) برای **هر کاربر واردشده** باز است. پس کسی که فقط `Equipments.Create` دارد هم می‌تواند فرم ثبت را پر کند. لیست و جزئیات همچنان `Equipments.View` می‌خواهند.

## تکنسین تجهیز

- **هر کاربری می‌تواند تکنسین باشد** ([ADR-0017](adr/0017-retire-technician-post.md)): سمت، معیار نیست و **فعال‌بودن** هم بررسی نمی‌شود؛ فقط **وجود** کاربر (`404 parent_not_found`، در ثبت، `bulk`، ثبت با فایل، بازگردانی، `move` و `bulk-move`، همه یکسان). dropdown (`GET /technicians`) فقط برای راحتی، همهٔ کاربران **فعال** را نشان می‌دهد.
- **تکنسین فعلی تجهیز نزد ستاد دست‌نخورده می‌ماند** حتی اگر سمت یا وضعیت آن کاربر بعداً عوض شود. تکنسین فقط با ثبت و `move` عوض می‌شود، پس ویرایش `PUT` به آن کاری ندارد. بازگردانی تجهیز غیرفعال فقط فعال بودن تکنسین قبلی را می‌خواهد، نه سمتش را.
- **`GET /api/Equipment/{id}/technician`:** `id`، `fullName`، `personnelCode`، `isActive`، `postId`، `postTitle`، `supportingDepartmentId` و `supportingDepartmentTitle`. تلفن، ایمیل و کد ملی عمداً نیست. تکنسینِ غیرفعال‌شده یا تجهیز غیرفعال هم نشان داده می‌شود (`isActive: false`). تاریخچه‌ی تکنسین‌های قبلی از `GET /{id}/location-history` می‌آید.

## قاعده‌های باز (منتظر تصمیم کسب‌وکار)

هر کدام در یک نقطه‌ی مشخص‌اند و عوض‌کردنشان به بقیه‌ی کد دست نمی‌زند:

| موضوع | الان | کجا عوض می‌شود |
|---|---|---|
| ترکیب معتبر شعبه/دپارتمان و تکنسین (مثلاً «نزد تکنسین» بدون تکنسین). «دقیقاً یکی از شعبه و دپارتمان» **بسته شده** و جزو این قاعده نیست | تکنسین فقط برای تجهیز نزد ستاد اختیاری است | `DepartmentTechnicianLocationRule` و قید Fluent جدول |
| انتقال‌های مجاز وضعیت | همه‌ی انتقال‌ها آزاد است | `EquipmentStatusTransitions.IsAllowed` |
| چه کسی تکنسین می‌شود | **هر کاربر موجود** (سمت و فعال‌بودن بررسی نمی‌شود)؛ dropdown همهٔ کاربران فعال | `EquipmentRepository.QueryTechnicians` و `FindExistingUserIdsAsync` ([ADR-0017](adr/0017-retire-technician-post.md)) |
| مانع غیرفعال‌سازی (مثلاً گزارش خرابی باز) | ندارد | `EquipmentService.DeleteAsync` |

وضعیت‌ها همان وضعیت‌های سیستمی `EquipmentStatusCode` هستند (شناسه = مقدار enum، [CATALOGS.md](CATALOGS.md)).

## پیوست‌ها

تجهیز پیوست‌پذیر است (`[Attachable]`): `GET/POST /api/Attachments/Equipments/{id}` و ... با `Equipments.View` و `Equipments.Update`. جزئیات در [ATTACHMENTS.md](ATTACHMENTS.md).

## هنوز در دامنه‌ی این فاز نیست

پر کردن جدول‌های فقط‌خواندنی `Zone`، `OrganizationManagement`، `Field`، `Organization` (از بیرون پر می‌شوند).

## قید تکنسین و ستاد (تصمیم ۳.۱۸)

`CK_Equipments_Technician_RequiresDepartment`: `[TechnicianUserId] IS NULL OR [CurrentSupportingDepartmentId] IS NOT NULL`.
با قید موجود «دقیقاً یک محل»، شعبه با تکنسین غیرمجاز و ستاد با یا بدون تکنسین مجاز است. قید جدید فقط روی `Equipments` است؛ تاریخچه‌های قدیمی تغییر نمی‌کنند. درخواست تکی ناسازگار با وضعیت نهایی `400 technician_requires_department` می‌گیرد؛ ثبت ورودی نامعتبر `validation_failed` و جابه‌جایی گروهی `bulk_items_invalid` است.

این قاعده بر توضیح‌های قبلی دربارهٔ حفظ تکنسین هنگام انتقال به شعبه مقدم است. migration و snapshot در این کامیت ساخته نشده‌اند و صاحب محصول آن‌ها را محلی می‌سازد و اجرا می‌کند. پیش از اجرا:

```sql
SELECT EquipmentId, CurrentOrganizationId, TechnicianUserId
FROM Equipments
WHERE CurrentOrganizationId IS NOT NULL AND TechnicianUserId IS NOT NULL;
```

اصلاح داده‌های ناسازگار باید صریح و توسط مالک داده انجام شود؛ migration نباید تکنسین‌ها را خودکار پاک کند.

## اجرای تست قید روی SQL Server

تست `SqlEquipmentLocationConstraintTests` با نوشتن مستقیم در SQL، هشت ترکیب شعبه/ستاد/تکنسین، ویرایش نامعتبر، انتقال صحیح با برداشت تکنسین و شکست migration در حضور دادهٔ قدیمی ناسازگار را بررسی می‌کند. دیتابیس هر تست نام مستقل با پیشوند `EMS_location_test_` دارد و در پایان حذف می‌شود. این تست‌ها بدون `EMS_TEST_SQL` اجرا نمی‌شوند.

از ریشهٔ مخزن در PowerShell، با حسابی که روی SQL آزمایشی اجازهٔ ساخت و حذف دیتابیس دارد:

```powershell
$env:EMS_TEST_SQL = 'Server=localhost;Database=EMS_constraints_test;Integrated Security=True;Encrypt=True'
dotnet test EMS.Tests/EMS.Tests.csproj --filter 'FullyQualifiedName~SchemaDriftTests|FullyQualifiedName~SqlEquipmentLocationConstraintTests|FullyQualifiedName~SqlMigrationStartupTests' --logger 'console;verbosity=normal'
Remove-Item Env:EMS_TEST_SQL
```

نام سرور و روش احراز هویت را با محیط خود هماهنگ کنید. اطلاعات ورود را در فایل‌های مخزن ذخیره نکنید. این دستور روی دیتابیس عملیاتی اجرا نشود: `SqlMigrationStartupTests` دیتابیس معرفی‌شده در اتصال را حذف و دوباره ایجاد می‌کند؛ نامش باید شامل `test` باشد. تست جدید تجهیزات نیز دیتابیس‌های مستقل موقت ایجاد می‌کند. از دیتابیس دارای اطلاعات واقعی استفاده نکنید.

migration جدید توسط صاحب محصول با شناسهٔ `20261111093443_SetConstraintForOrganizationSupoortingDepartmentTechnecianInEquipment` ثبت شده است؛ تست تطابق مدل باید پس از دریافت این migration پاس شود.
