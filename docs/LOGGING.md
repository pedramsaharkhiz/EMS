# Logging در EMS

این سند توضیح می‌دهد سیستم چه چیزهایی را لاگ می‌کند، لاگ‌ها کجا ذخیره می‌شوند، چطور در آن‌ها جستجو کنیم و توسعه‌دهنده‌ها هنگام نوشتن کد چه قواعدی را رعایت کنند.

## ساختار کلی

```
درخواست HTTP
  │
  ├─ CorrelationIdMiddleware      شناسه‌ی ردگیری (X-Correlation-ID) را می‌سازد یا از کلاینت می‌گیرد
  ├─ Serilog Request Logging      یک خط برای هر درخواست: متد، مسیر، status، زمان، کاربر، IP
  ├─ GlobalExceptionMiddleware    خطاها را به پاسخ JSON تبدیل و با سطح مناسب لاگ می‌کند
  ├─ Authentication
  ├─ UserLogContextMiddleware     UserId و UserName را به همه‌ی لاگ‌های ادامه‌ی درخواست اضافه می‌کند
  └─ Authorization → Controller → Service → Repository / EF Core
```

لاگ‌ها در سه دسته نوشته می‌شوند (فیلد `SourceContext`):

| دسته | محتوا |
|---|---|
| `EMS.Security` | ورود، خروج، refresh token، قفل شدن حساب، رد شدن دسترسی، خطای احراز هویت |
| `EMS.Audit` | عملیات مدیریتی: ایجاد، ویرایش و حذف موجودیت‌ها، تغییر رمز، تخصیص نقش، تغییر permission |
| سایر | لاگ عمومی برنامه، درخواست‌ها، خطاها، EF Core، startup |

## فایل‌های لاگ

مسیر پیش‌فرض: پوشه‌ی `logs/` کنار برنامه (نسبت به مسیر اجرای برنامه). قالب فایل‌ها JSON فشرده (Serilog CLEF) و هر خط یک رویداد است.

| فایل | محتوا | نگهداری |
|---|---|---|
| `logs/ems-YYYYMMDD.log` | همه‌ی لاگ‌ها | ۳۰ روز |
| `logs/ems-security-YYYYMMDD.log` | فقط `EMS.Security` و `EMS.Audit` | ۹۰ روز |

خروجی Console هم فعال است و قالب خوانا دارد: `[22:07:49 WRN] [<correlation-id>] <دسته>: <پیام>`.

تنظیمات (سطح لاگ، مسیر، مدت نگهداری) در بخش `Serilog` فایل `appsettings.json` است. در محیط Production فایل `appsettings.Production.json` سطح `Microsoft` و `System` را روی `Warning` می‌گذارد و در Development سطح پیش‌فرض `Debug` است.

> نکته: برنامه باید به پوشه‌ی لاگ دسترسی نوشتن داشته باشد. فایل‌ها با `shared: true` باز می‌شوند تا هنگام recycle شدن (دو پردازش هم‌زمان) مشکلی پیش نیاید.

## سطح لاگ

| وضعیت | سطح |
|---|---|
| درخواست موفق (۲xx و ۳xx) | `Information` |
| پاسخ ۴xx | `Warning` (در request log) |
| خطای مورد انتظار کلاینت (NotFound، Conflict، BadRequest، Validation) | `Information` (بدون stack trace) |
| ۴۰۱ ناشی از `UnauthorizedAccessException` | `Warning` در `EMS.Security` |
| رد شدن دسترسی (۴۰۳) | `Warning` در `EMS.Security` |
| استفاده‌ی مجدد از refresh token ابطال‌شده (احتمال سرقت توکن) | `Error` در `EMS.Security` |
| خطای پیش‌بینی‌نشده (۵۰۰) | `Error` همراه با exception کامل |
| لغو درخواست توسط کلاینت | `Information` (status ثبت‌شده ۴۹۹، نه خطای سرور) |
| کوئری دیتابیس کندتر از آستانه | `Warning` |
| `/health/*`، `/scalar`، `/openapi` | `Verbose` (در عمل نوشته نمی‌شود) |

## فیلدهای مشترک

هر لاگ مربوط به یک درخواست این فیلدها را دارد:

- `CorrelationId`: همان مقدار هدر `X-Correlation-ID` پاسخ. کلاینت می‌تواند خودش بفرستد (۸ تا ۶۴ کاراکتر از `A-Z a-z 0-9 - _ . :`). مقدار نامعتبر جایگزین می‌شود. پاسخ‌های خطا هم فیلد `correlationId` دارند، پس کاربر می‌تواند آن را به پشتیبانی بدهد.
- `UserId` و `UserName` برای درخواست‌های احراز هویت‌شده.
- `ClientIp` و `UserAgent`.
- `MachineName`، `Application`، `Environment`، `ThreadId`.

> پشت reverse proxy یا load balancer `ClientIp` آدرس proxy خواهد بود، مگر اینکه `UseForwardedHeaders` پیکربندی شود. این کار فعلاً انجام نشده چون به تنظیم زیرساخت وابسته است.

## رویدادهای امنیتی (`EMS.Security`)

| رویداد | سطح | توضیح |
|---|---|---|
| `Login succeeded` | Information | کاربر، IP، User-Agent |
| `Login failed (Reason)` | Warning | `Reason` یکی از `UnknownUser`، `InactiveUser`، `InvalidPassword`، `LockedOut` |
| `Refresh token rotated` | Information | |
| `Refresh token rejected (Reason)` | Warning | `TokenNotFound`، `TokenExpired`، `TokenInactive`، `UserInvalid` |
| `Refresh token rejected (TokenReuseDetected)` | Error | توکن قبلاً ابطال شده بود و دوباره استفاده شد |
| `Refresh token revoked by request` | Information | |
| `Access denied: user ... lacks permission X` | Warning | کاربر لاگین‌شده بدون permission لازم (۴۰۳) |
| `Authentication failed (reason)` | Warning | توکن ارسال شده ولی رد شد: منقضی، امضای نامعتبر، ... (خود توکن هرگز لاگ نمی‌شود) |

دلیل دقیق شکست ورود فقط در لاگ ثبت می‌شود. پاسخ به کلاینت همیشه پیام عمومی «نام کاربری یا کلمه عبور نامعتبر است» است تا امکان user enumeration نباشد.

### رد شدن دسترسی (۴۰۳)

هر action ای که `[Permission(...)]` دارد، هنگام رد شدن یک لاگ `Access denied` با نام دقیق permission، کاربر، متد و مسیر می‌نویسد. همه‌ی endpointها به‌جز `Login`، `RefreshToken` و `RevokeRefreshToken` اکنون permission اختصاصی دارند، از جمله هشت کنترلر داده‌های پایه. هر کدام چهار permission دارند:

| منبع | permissionها |
|---|---|
| Brand | `Brands.View/Create/Update/Delete` |
| EquipmentCategory | `EquipmentCategories.*` |
| EquipmentLocationType | `EquipmentLocationTypes.*` |
| EquipmentModel | `EquipmentModels.*` |
| EquipmentStatus | `EquipmentStatuses.*` |
| EquipmentType | `EquipmentTypes.*` |
| FailureCategory | `FailureCategories.*` |
| SupportingDepartment | `SupportingDepartments.*` |

نقش `Admin` در هر بار راه‌اندازی برنامه همه‌ی permissionهای جدید را خودکار می‌گیرد (seeder). برای نقش‌های دیگر باید این permissionها را از طریق `POST /api/Role/AssignPermissions` تخصیص دهید، و کاربران باید دوباره وارد شوند (permissionها داخل access token هستند).

یک تست (`PermissionCatalogTests`) تضمین می‌کند هیچ action جدیدی بدون `[Permission]` ثبت‌شده در `Permissions.All` اضافه نشود. در غیر این‌صورت دسترسی آن نه کنترل می‌شود و نه رد شدنش لاگ می‌شود.

## چرا لاگ امنیتی در فایل نوشته می‌شود و نه در دیتابیس

1. **باید وقتی دیتابیس مشکل دارد هم کار کند.** رویدادهای امنیتی دقیقاً در شرایط بحرانی مهم‌اند: حمله، قطع شدن SQL Server، تراکنش rollback‌شده. اگر لاگ ورود ناموفق در همان دیتابیسی نوشته شود که ممکن است در دسترس نباشد، همان لحظه‌ای که لازمش دارید از دست می‌رود. نوشتن در فایل به هیچ سرویس دیگری وابسته نیست.
2. **هزینه و ریسک حمله.** هر تلاش ورود، refresh و ۴۰۳ یک رویداد است. حمله‌ی brute-force هزاران رویداد در دقیقه می‌سازد. اگر هرکدام یک INSERT باشد، مهاجم می‌تواند دیتابیس را زیر بار ببرد و جدول را متورم کند. افزودن به فایل بسیار ارزان‌تر است.
3. **تراکنش.** رویدادی مثل «ورود ناموفق» معمولاً در درخواستی رخ می‌دهد که خطا می‌دهد یا تراکنشش برگشت می‌خورد. ردیف دیتابیسی که در همان تراکنش بود همراه آن از بین می‌رود. فایل از تراکنش مستقل است.
4. **اتصال به ابزار استاندارد.** فایل JSON ساختاریافته را ابزارهایی مثل `jq`، Seq، ELK و Grafana Loki مستقیم می‌خوانند و می‌توان آن را به یک سیستم متمرکز و غیرقابل‌تغییر فرستاد. چرخش روزانه و مدت نگهداری جدا (۹۰ روز برای امنیتی) هم رایگان است.
5. **دو چیز متفاوت.** *لاگ امنیتی* می‌گوید «چه اتفاقی افتاد» (تلاش ورود، رد دسترسی) و *Audit Trail دیتابیس* می‌گوید «چه داده‌ای چطور عوض شد». دومی به همین دلیل در دیتابیس است: باید کنار خود داده، در همان تراکنش و قابل join و گزارش‌گیری باشد (بخش بعد). تغییر نقش‌ها و permissionها هم، وقتی Audit Trail روشن باشد، در هر دو جا ثبت می‌شود.

محدودیت‌ها: فایل روی سرور برنامه است. کسی که به آن سرور دسترسی کامل بگیرد می‌تواند فایل را هم دست‌کاری کند. برای مقاومت در برابر این حالت لاگ‌ها باید به سیستم دیگری فرستاده شوند (Seq، ELK یا SIEM). در چند سرور هم باید لاگ‌ها جمع‌آوری شوند. اگر صفحه‌ی داخل برنامه برای مشاهده‌ی رویدادهای امنیتی لازم شد، می‌توان یک sink دیتابیس (مثلاً `Serilog.Sinks.MSSqlServer` با batching) *علاوه بر* فایل افزود، نه به‌جای آن.

## رویدادهای Audit (`EMS.Audit`)

فرمت: `<EntityType> <EntityId> created|updated|deleted by user <ActorId> (<ActorName>)` و برای عملیات خاص `<Operation> on <EntityType> <EntityId> by user ...`. عملیات خاص:

`UserDeactivated`، `PasswordReset` (فقط خود رویداد، نه رمز)، `RefreshTokensRevoked`، `RolesSet`، `RoleAssigned`، `RoleRemoved`، `RoleUsersSet`، `PermissionsChanged` (با فهرست permissionهای اضافه و حذف‌شده).

## Audit Trail در دیتابیس (جدول `AuditLogs`)

علاوه بر لاگ فایل، می‌توان برای هر insert، update و delete مقدار قبلی و جدید را در جدول `AuditLogs` نگه داشت: `TableName`، `EntityId`، `Action`، `OldValues`، `NewValues`، `ChangedColumns`، `UserId`، `UserName`، `IpAddress`، `CorrelationId`، `TimestampUtc`.

**این قابلیت به‌صورت پیش‌فرض خاموش است** (`Logging:AuditTrail:Enabled = false`)، چون جدول باید اول در دیتابیس ساخته شود. اگر پرچم روشن شود و جدول وجود نداشته باشد، عملیات اصلی خراب نمی‌شود ولی هر بار خطای «Failed to write audit trail row(s)» لاگ می‌شود.

فعال‌سازی:

1. ساخت جدول، به یکی از دو روش:
   - اجرای اسکریپت [`docs/sql/AddAuditLog.sql`](sql/AddAuditLog.sql) روی دیتابیس (idempotent است و چندبار اجرا کردنش بی‌ضرر است)؛
   - یا `dotnet ef database update --project EMS.Infrastructure --startup-project EMS.Presentation`.
2. تنظیم `"Logging": { "AuditTrail": { "Enabled": true } }` در appsettings همان محیط.
3. راه‌اندازی مجدد برنامه.

نکات:

- جدول بعد از ذخیره‌ی تغییر اصلی نوشته می‌شود (تا شناسه‌های تولیدشده توسط دیتابیس معلوم باشند). اگر نوشتن جدول شکست بخورد، عملیات کاربر برگردانده نمی‌شود و خطا لاگ می‌شود. در تراکنش‌های صریح (`BeginTransaction`) ردیف audit در همان تراکنش نوشته می‌شود.
- ستون‌های `Password*`، `*Secret*`، `*Token*` و `SecurityStamp` با `***` ماسک می‌شوند. موجودیت‌های `AuditLog` و `UserRefreshToken` هرگز audit نمی‌شوند.
- ستون‌های `ModifiedDateTime`، `ModifiedByUserId` و `ConcurrencyStamp` در لیست تغییرات نمی‌آیند. به‌روزرسانی‌ای که هیچ ستونی را عوض نکند ثبت نمی‌شود.
- حجم جدول با استفاده رشد می‌کند. برای آن سیاست نگهداری (پاک‌سازی یا آرشیو دوره‌ای) در نظر بگیرید.

نمونه‌ی کوئری: همه‌ی تغییرات یک درخواست خاص

```sql
SELECT TimestampUtc, TableName, EntityId, Action, ChangedColumns, UserName
FROM AuditLogs
WHERE CorrelationId = '<correlation-id>'
ORDER BY Id;
```

## کوئری کند

دستورهای دیتابیس کندتر از `Logging:SlowQueryThresholdMs` (پیش‌فرض ۵۰۰ میلی‌ثانیه) با سطح `Warning` لاگ می‌شوند: متن SQL (حداکثر ۲۰۰۰ کاراکتر) و مدت اجرا. **مقدار پارامترها هرگز لاگ نمی‌شود** و `EnableSensitiveDataLogging` خاموش است.

## جستجو در لاگ‌ها

فایل‌ها JSON هستند. با [`jq`](https://jqlang.github.io/jq/):

```bash
# همه‌ی لاگ‌های یک درخواست (از روی correlation id که کاربر گزارش داده)
jq -c 'select(.CorrelationId == "<id>")' logs/ems-*.log

# ورودهای ناموفق امروز
jq -c 'select(.["@mt"] | startswith("Login failed"))' logs/ems-security-$(date +%Y%m%d).log

# خطاهای سرور
jq -c 'select(.["@l"] == "Error")' logs/ems-*.log

# تغییرات یک کاربر مشخص
jq -c 'select(.SourceContext == "EMS.Audit" and .ActorId == 5)' logs/ems-security-*.log
```

فیلدهای استاندارد CLEF: `@t` زمان (UTC)، `@mt` الگوی پیام، `@l` سطح (در سطح Information حذف می‌شود)، `@x` exception.

## قواعد برای توسعه‌دهنده‌ها

1. **هرگز** رمز، توکن (access/refresh)، hash، کلید یا بدنه‌ی کامل درخواست را لاگ نکنید. DTOهایی مثل `LoginRequestDto` و `SetUserPasswordDto` را با `{@Dto}` لاگ نکنید.
2. از message template استفاده کنید، نه string interpolation: `_logger.LogInformation("Order {OrderId} shipped", id)` و نه `$"Order {id} shipped"`. در حالت اول فیلد `OrderId` قابل جستجو است.
3. هر مقداری که کاربر می‌فرستد (نام کاربری، پیام exception) قبل از لاگ شدن باید با `LogSanitizer.Sanitize(...)` عبور کند تا کاراکترهای کنترلی (جعل خط لاگ) خنثی شوند و طولش محدود شود.
4. رویدادهای امنیتی را با `ISecurityEventLogger` و عملیات مدیریتی را با `IAuditEventLogger` بنویسید، نه با `ILogger` مستقیم. این‌ها دسته‌ی صحیح (`EMS.Security`/`EMS.Audit`) و فیلدهای هماهنگ را تضمین می‌کنند.
5. خطای مورد انتظار را با `Information` یا `Warning` و خطای واقعی را با `LogError(exception, ...)` همراه exception بنویسید. exception را به‌صورت `ex.Message` در پیام ننویسید.
6. در لایه‌های Domain و Application فقط از `Microsoft.Extensions.Logging` (`ILogger<T>`) استفاده کنید، نه Serilog.

## تست

```bash
dotnet test EMS.Tests/EMS.Tests.csproj
```

تست‌ها شامل تست واحد (sanitizer، correlation id، mapping استثناها به status و سطح لاگ، audit interceptor) و تست یکپارچه‌ی کامل با `WebApplicationFactory` روی دیتابیس InMemory هستند. از جمله: ورود موفق و ناموفق، قفل شدن حساب، replay توکن refresh، ۴۰۱ و ۴۰۳، هدر correlation، ردیف‌های audit، فایل‌های JSON لاگ، و اسکن تمام لاگ‌ها برای اطمینان از اینکه رمز و توکن در آن‌ها نیست.

> محدودیت: تست‌ها روی SQL Server واقعی اجرا نمی‌شوند. اجرای migration و رفتار کوئری کند روی دیتابیس واقعی باید جداگانه بررسی شود.
