# پیوست‌ها (Attachments)

پیوست یک قابلیت **کلی** برای همه‌ی موجودیت‌هاست: یک جدول `EntityAttachments` به‌همراه جدول ثبت `Tables`. فایل‌ها **داخل دیتابیس SQL Server** ذخیره می‌شوند (به‌صورت stream) و **هیچ فایلی روی دیسک سرور برنامه نمی‌نشیند**.

## ساختار داده

| جدول | کار |
|---|---|
| `Tables` | فهرست موجودیت‌های سیستم. `IsAttachmentEnabled` می‌گوید پیوست برای این جدول فعال است یا نه |
| `EntityAttachments` | «جدول X، رکورد Y، این فایل». ستون‌های `TableId`، `EntityId` (بدون کلید خارجی، چون به جدول‌های مختلف اشاره می‌کند)، `StreamId` (کلید خارجی به محتوا)، `Description`، `IsActive` |
| `AttachmentContents` | خود فایل: `StreamId` (کلید)، `FileName`، `ContentType`، `SizeBytes`، `Sha256` و ستون `Content` از نوع `varbinary(max)` |

- `Content` عمداً property کلاس C# نیست (ستون سایه)، پس هیچ کدی نمی‌تواند فایل را اتفاقی در حافظه بخواند. فقط `IAttachmentBlobStore` آن را به‌صورت stream می‌نویسد و می‌خواند.
- چون `EntityId` کلید خارجی ندارد، سرویس وجود رکورد (و فعال بودنش) را خودش چک می‌کند (`404` اگر نباشد).
- حذف پیوست = `IsActive = false` روی ردیف `EntityAttachments`. فایل می‌ماند، ولی از لیست پیش‌فرض می‌رود و دانلودش `404` می‌شود.

### چرا `varbinary(max)` و نه FileTable؟
سرور فعلی SQL Server 2022 Enterprise روی Windows است ولی FILESTREAM روشن نیست (`FilestreamEffectiveLevel = 0`)؛ روشن کردنش به ریستارت سرویس SQL و کار DBA نیاز دارد، ساخت FileTable با EF ممکن نیست (SQL خام لازم است) و روی Linux و Azure SQL کار نمی‌کند. `varbinary(max)` روی هر نسخه کار می‌کند، با migration معمولی ساخته می‌شود، کلید خارجی واقعی دارد، در تراکنش و backup معمولی می‌ماند و آپلود و دانلودش stream است. هزینه: حجم فایل‌ها داخل فایل‌های دیتابیس می‌رود. اگر روزی FILESTREAM لازم شد، فقط `IAttachmentBlobStore` عوض می‌شود.

## موجودیت پیوست‌پذیر

یک موجودیت با یک attribute پیوست‌پذیر می‌شود، بدون کد دیگر:

```csharp
[Attachable("تجهیز", "Equipments")]      // نام فارسی، گروه permission
public class Equipment : BaseEntity, IAuditableEntity { ... }
```

- **جدول `Tables` خودکار پر می‌شود:** در هر راه‌اندازی `AttachmentTableSeeder` همه‌ی موجودیت‌های `[Attachable]` را پیدا می‌کند و برایشان ردیف می‌سازد یا اصلاح می‌کند (`IsAttachmentEnabled = true`). موجودیتی که attribute را از دست داد فقط **غیرفعال** می‌شود، پاک نمی‌شود (پیوست‌های قدیمی به ردیف اشاره می‌کنند). idempotent است.
- **permissionها از خود موجودیت می‌آیند:** `{Group}.View` برای لیست و دانلود، `{Group}.Update` برای آپلود و غیرفعال‌سازی. برای تجهیز: `Equipments.View` و `Equipments.Update`. هر دو باید در `Permissions` باشند (تست `Every_attachable_entity_has_view_and_update_permissions...` این را می‌گیرد).
- کلید اصلی موجودیت باید یک `long` باشد (تست دارد).
- فعلاً فقط `Equipment` نشانه‌دار است.

## endpointها

مسیر `GET/POST /api/Attachments/{tableName}/{entityId}` که `tableName` نام جدول است (مثلاً `Equipments`).

| متد و مسیر | permission | کار |
|---|---|---|
| `GET /api/Attachments/{tableName}/{entityId}` | `{Group}.View` | لیست پیوست‌های یک رکورد، جدیدترین اول، صفحه‌بندی‌شده. `?isActive=false` غیرفعال‌ها را نشان می‌دهد |
| `POST /api/Attachments/{tableName}/{entityId}?description=` | `{Group}.Update` | آپلود یک فایل (`201` + هدر `Location` به آدرس دانلود) |
| `GET /api/Attachments/{id}/download` | `{Group}.View` | دانلود فایل (stream) |
| `DELETE /api/Attachments/{id}` | `{Group}.Update` | غیرفعال‌سازی |

### آپلود
بدنه‌ی `multipart/form-data` با **فایل در بخش فایلی** (فقط اولین فایل خوانده می‌شود). توضیح اختیاری در **query string** (`?description=`) می‌آید تا بخش فایل بدون بافر شدن stream شود.

```
POST /api/Attachments/Equipments/42?description=نمای%20کلی
Content-Type: multipart/form-data; boundary=...
Authorization: Bearer <token>
```

بدنه در طول مسیر **stream** می‌شود: نه در حافظه جمع می‌شود و نه روی دیسک سرور برنامه. (برای همین attribute `[DisableFormValueModelBinding]` روی action هست؛ بدون آن، MVC کل فرم را پیش از اجرای action بافر می‌کرد.)

### دانلود
پاسخ خام فایل است، نه پوشش JSON (`[FileDownloadResponse]`، مثل Excel یک استثنای مصوب قاعده‌ی پوشش). `Content-Type` همان است که هنگام پذیرش از روی پسوند ذخیره شده، و `Content-Disposition: attachment` است. خطاها همچنان پوشش JSON فارسی‌اند.

## پیوست‌ها در جزئیات و لیست تجهیز

فرانت برای نمایش پیوست‌های یک تجهیز دیگر درخواست دوم لازم ندارد:

- **`GET /api/Equipment/{id}`** علاوه بر فیلدهای جزئیات، `attachments` را برمی‌گرداند: آرایه‌ی پیوست‌های **فعال**، جدیدترین اول (حداکثر ۲۰، پس صفحه‌بندی ندارد). هر عضو `AttachmentItemDto` است: `id`، `tableName`، `entityId`، `fileName`، `contentType`، `sizeBytes`، `description`، `isActive`، `createdDateTime`، `createdByUserId`. **خود فایل در پاسخ نیست؛** با `GET /api/Attachments/{id}/download` دانلود می‌شود. پیوست غیرفعال‌شده نمی‌آید (برای دیدنش `GET /api/Attachments/Equipments/{id}?isActive=false`).
- **تجهیز غیرفعال** هم پیوست‌هایش را نشان می‌دهد (جزئیاتش هم قابل دیدن است). مجوز دیدن پیوست همان `Equipments.View` است، پس جدا بررسی نمی‌شود.
- **`attachmentCount`** (تعداد پیوست‌های فعال) در هر ردیف `GET /api/Equipment` (لیست) و در هر پاسخی که جزئیات تجهیز دارد (جزئیات، ویرایش، `move`، ثبت) هست. در لیست از **یک کوئری گروهی برای کل صفحه** می‌آید، نه یک کوئری برای هر ردیف. لیست خود فایل‌ها را نمی‌آورد.
- پاسخ **ویرایش** و **`move`** فقط `attachmentCount` دارند و `attachments` ندارند (جزئیات و ثبت با فایل دارند). برای فهرست کامل بعد از ویرایش، همان `GET /{id}`.
- پاسخ **ثبت با فایل** همان شکل `GET /{id}` است، پس فرانت یک نوع را برای هر دو استفاده می‌کند.

## ثبت تجهیز همراه با پیوست (یک درخواست)

برای اینکه فرانت هم بتواند تجهیز را **بدون** پیوست و هم **با** پیوست ثبت کند، یک endpoint جدا هست. `POST /api/Equipment` همان‌طور که بود می‌ماند و تغییری نکرده است.

`POST /api/Equipment/with-attachments` (مجوز `Equipments.Create`؛ `Update` لازم نیست)

بدنه `multipart/form-data` است و مثل آپلود معمولی **stream** می‌شود (نه در حافظه جمع می‌شود و نه روی دیسک سرور). ترتیب بخش‌ها مهم است:

1. **بخش اول:** بخش متنی به نام `equipment` با **همان JSON** بدنه‌ی `POST /api/Equipment` (همان فیلدها، همان قواعد و همان خطاها).
2. **بعد از آن:** هر تعداد فایل (حداکثر ۵). یک بخش متنی به نام `description` که **درست قبل از یک فایل** بیاید، توضیح همان فایل است (اختیاری). بخش‌های دیگر نادیده گرفته می‌شوند.

```js
const form = new FormData();
form.append("equipment", JSON.stringify({ yaasCode: "Y-1001", owner: 1, equipmentTypeId: 3, equipmentStatusId: 1, currentSupportingDepartmentId: 2 }));  // اول
form.append("description", "نمای کلی");     // اختیاری، برای فایل بعدی
form.append("file", fileA);
form.append("file", fileB);
await fetch("/api/Equipment/with-attachments", { method: "POST", headers: { Authorization: `Bearer ${token}` }, body: form });
```

- **همه یا هیچ:** تجهیز، تاریخچه‌ی مکانش و همه‌ی فایل‌ها در **یک تراکنش** ذخیره می‌شوند. اگر هر فایلی رد شود یا هر مرحله‌ای خطا بدهد، هیچ تجهیز و فایلی نمی‌ماند. رکورد audit هم فقط بعد از commit نوشته می‌شود.
- **پیوست اختیاری است:** بدون فایل، ثبت معمولی است و `attachments: []` برمی‌گردد.
- **حدها:** حداکثر **۵ فایل** و **۵۰ مگابایت** کل (`AttachmentRules.MaxFilesPerRegistration` و `MaxRegistrationBytes`). هر فایل همچنان ≤ ۱۰ مگابایت و از همان نوع‌های مجاز است. فایل ششم `400 attachments_per_request_exceeded` می‌دهد (و کل ثبت لغو می‌شود). درخواست بزرگ‌تر از ۵۱ مگابایت `413`.
- **پاسخ:** `201` با هدر `Location` و بدنه‌ی `EquipmentRegistrationDto`: همه‌ی فیلدهای `EquipmentDetailDto` (پس فرانت همان نوع ثبت معمولی را استفاده می‌کند) به‌علاوه‌ی `attachments` (فهرست پیوست‌های ذخیره‌شده، به ترتیب ارسال).
- **خطاها:** JSON تجهیز اول خوانده و اعتبارسنجی می‌شود و اگر خطا داشته باشد هیچ فایلی خوانده نمی‌شود: `400 validation_failed` با همان فیلدهای ثبت معمولی. اگر بخش اول `equipment` نباشد یا JSON نامعتبر باشد: `400 validation_failed` با فیلد `equipment`. خطاهای فایل همان کدهای پیوست‌اند (`attachment_type_not_allowed`، `attachment_content_mismatch`، `attachment_too_large`). بدنه‌ی غیر multipart: `415`.
- **ثبت گروهی (`/bulk`) فایل نمی‌گیرد.** برای ۲۰۰ تجهیز با فایل، نه سقف حجم می‌خواند و نه مدل.
- **اجرا در دیتابیس:** پیش از شروع تراکنش، تراکنش باز می‌ماند تا آپلود فایل‌ها تمام شود (چند ثانیه برای فایل‌های بزرگ). ردیف تجهیز تا آن لحظه قفل است.

## قواعد و حدها

| قاعده | مقدار |
|---|---|
| حداکثر اندازه‌ی هر فایل | ۱۰ مگابایت (`AttachmentRules.MaxFileSizeBytes`) |
| حداکثر پیوست فعال هر رکورد | ۲۰ (`MaxActivePerEntity`) |
| طول توضیح / نام فایل | ۵۰۰ / ۲۰۰ کاراکتر |
| نوع‌های مجاز | `pdf`، `jpg`، `jpeg`، `png`، `docx`، `xlsx`، `txt` |

- **نوع فایل دو بار بررسی می‌شود:** پسوند باید مجاز باشد **و** بایت‌های ابتدای فایل (magic bytes) باید با آن بخواند (PDF با `%PDF-`، PNG و JPEG با امضایشان، docx و xlsx با امضای zip، txt بدون بایت NUL و کنترلی). `MIME` ادعایی کلاینت هرگز استفاده نمی‌شود؛ نوعی که ذخیره و سرو می‌شود از جدول `AttachmentRules` می‌آید. فایل `.exe` که `.pdf` نام گرفته رد می‌شود.
- **نام فایل پاک‌سازی می‌شود:** مسیر (`../`، `C:\`) و نویسه‌های رزروشده (`< > | : * ? "`) و کنترلی حذف می‌شود؛ نام فارسی می‌ماند.
- **اندازه حین stream کنترل می‌شود:** فایل بزرگ‌تر از حد، همان‌جا رد می‌شود و تراکنش برمی‌گردد؛ هیچ ردیف یا فایلی نمی‌ماند.
- **آپلود در یک تراکنش:** ردیف محتوا، stream فایل و ردیف پیوست با هم commit می‌شوند یا هیچ‌کدام.
- سرور خودش اندازه (`DATALENGTH`) و هش (`HASHBYTES('SHA2_256')`) فایل ذخیره‌شده را حساب می‌کند.

## کدهای خطا
| status | code | وقتی |
|---|---|---|
| 404 | `attachment_target_not_found` | جدول ناشناخته یا پیوستش غیرفعال است، یا رکورد وجود ندارد یا غیرفعال است |
| 404 | `not_found` | پیوست وجود ندارد یا غیرفعال شده |
| 400 | `attachment_file_required` | فایل ارسال نشده، خالی است یا نام فایل معتبر نیست |
| 400 | `attachment_type_not_allowed` | پسوند مجاز نیست |
| 400 | `attachment_content_mismatch` | محتوا با پسوند نمی‌خواند |
| 400 | `attachment_too_large` | فایل از حد بزرگ‌تر است |
| 409 | `attachment_limit_reached` | رکورد ۲۰ پیوست فعال دارد |
| 400 | `validation_failed` | توضیح بلندتر از ۵۰۰ کاراکتر (فیلد `description`) |
| 413 | `payload_too_large` | کل درخواست از حد endpoint بزرگ‌تر است (۱۰ مگابایت + ۱ مگابایت سربار) |
| 415 | `unsupported_media_type` | بدنه `multipart/form-data` نیست |
| 401 / 403 | | توکن نیست، یا permission موجودیت را ندارد |

## استقرار
- migration `AttachmentContents` (اسکریپت idempotent: `docs/sql/AttachmentContents.sql`): جدول `AttachmentContents`، کلید خارجی `EntityAttachments.StreamId`، ایندکس لیست. چون API پیوست قبلاً نبود، `EntityAttachments` باید خالی باشد؛ در سرآیند اسکریپت کوئری بررسی هست.
- جدول `Tables` موقع راه‌اندازی خودکار پر می‌شود؛ کاری لازم نیست.
- **حجم دیتابیس:** فایل‌ها داخل دیتابیس‌اند، پس اندازه‌ی فایل داده و backup رشد می‌کند. رشد را زیر نظر بگیر و برای دیتابیس‌های خیلی بزرگ به FILESTREAM فکر کن.
- **reverse proxy:** Kestrel و IIS پیش‌فرض حدود ۳۰ مگابایت دارند و endpoint خودش `RequestSizeLimit` ۱۱ مگابایت دارد. اگر جلوی برنامه nginx یا مشابه است، `client_max_body_size` باید حداقل ۱۱ مگابایت باشد.
- حدها ثابت‌های `AttachmentRules` هستند؛ تغییر آن‌ها یک خط است (به‌جز `RequestSizeLimit` که از همان ثابت می‌آید).

## تست
- تست‌های API (`AttachmentTests`) روی دیتابیس in-memory اجرا می‌شوند، که SQL خام و stream را نمی‌تواند اجرا کند؛ برای همین `InMemoryAttachmentBlobStore` جایگزین می‌شود (بایت‌ها در حافظه‌ی تست). قواعد، permissionها، حدها و seeder با این‌ها پوشش داده شده‌اند.
- **ثبت تجهیز با پیوست:** تست‌های API (`EquipmentWithAttachmentsTests`) روی in-memory اجرا می‌شوند که تراکنش را نادیده می‌گیرد؛ پس «فایل ردشده هیچ تجهیزی باقی نمی‌گذارد» فقط با `SqlEquipmentRegistrationTests` روی SQL Server واقعی (با `EMS_TEST_SQL`) اثبات می‌شود. تا وقتی آن تست اجرا نشده، این ویژگیِ اتمی‌بودن راستی‌آزمایی نشده است.
- **مسیر SQL** (`SqlAttachmentBlobStore`: stream به SQL، `OUTPUT` اندازه و هش، دانلود با reader، rollback) فقط روی SQL Server واقعی قابل راستی‌آزمایی است: `SqlAttachmentBlobStoreTests`. به‌صورت پیش‌فرض Skip است. برای اجرا، یک دیتابیس **موقت** با نامی که `test` دارد بساز، connection string را در `EMS_TEST_SQL` بگذار و تست‌ها را اجرا کن (دیتابیس را drop و دوباره می‌سازد).
