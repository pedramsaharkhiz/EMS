# رمزها و کلیدها

هیچ رمز یا کلیدی در مخزن نیست. برنامه آن‌ها را از **متغیر محیطی** (در سرور) یا **user-secrets** (روی سیستم توسعه) می‌خواند. اگر یکی نباشد، برنامه با پیام روشن بالا نمی‌آید (کلید JWT در حالت Development استثناست).

| تنظیم | متغیر محیطی | چه می‌شود اگر نباشد |
|---|---|---|
| رشته‌ی اتصال دیتابیس | `ConnectionStrings__EMS` | برنامه هنگام اولین اتصال خطا می‌دهد |
| کلید امضای توکن | `Jwt__SecretKey` | برنامه بالا نمی‌آید. حداقل ۳۲ بایت؛ ۶۴ کاراکتر تصادفی پیشنهاد می‌شود. مقدار قدیمیِ منتشرشده در مخزن رد می‌شود |
| رمز مدیر اولیه | `Seed__AdminPassword` | کاربر `admin` **ساخته نمی‌شود** و هشدار لاگ می‌شود. اگر `admin` از قبل هست، این تنظیم بی‌اثر است |

## ساخت یک کلید تصادفی
```bash
openssl rand -base64 48
```

## روی سیستم توسعه (user-secrets)
```bash
cd EMS.Presentation
dotnet user-secrets set "ConnectionStrings:EMS" "<رشته‌ی اتصال>"
dotnet user-secrets set "Jwt:SecretKey" "<کلید تصادفی>"
dotnet user-secrets set "Seed:AdminPassword" "<رمز دلخواه>"
```
در Development اگر `Jwt:SecretKey` تنظیم نشده باشد، برای همان اجرا یک کلید تصادفی ساخته می‌شود (با هر بار اجرای دوباره، توکن‌های قبلی باطل می‌شوند).

برای ابزار EF (`dotnet ef ...`) هم رشته‌ی اتصال لازم است: یا user-secrets بالا، یا
`ConnectionStrings__EMS="..." dotnet ef database update --project EMS.Infrastructure --startup-project EMS.Presentation`.

## روی سرور
متغیرهای بالا را در تنظیمات سرویس/استخر برنامه (IIS: Environment Variables، systemd: `Environment=`، Docker: `-e`) بگذار، نه در فایل داخل پوشه‌ی برنامه. در رشته‌ی اتصال Production از `Encrypt=True` و گواهی معتبر استفاده کن و `TrustServerCertificate=true` نگذار (آن گزینه گواهی سرور را بررسی نمی‌کند، پس جلوی حمله‌ی مرد میانی را نمی‌گیرد).

## چیزهایی که باید یک‌بار دستی انجام شود
مقدارهای قدیمی در **تاریخچه‌ی گیت** هستند و با حذف از فایل پاک نمی‌شوند؛ همه‌ی آن‌ها را «لو رفته» حساب کن:
1. کلید JWT را عوض کن (همه‌ی توکن‌های فعلی باطل می‌شوند).
2. رمز کاربر دیتابیس را عوض کن.
3. رمز کاربر `admin` را عوض کن. اگر هنوز رمز قدیمیِ منتشرشده را دارد، برنامه هنگام شروع یک لاگ `Critical` می‌نویسد: `SECURITY: the administrator ... still has the password that was published`. تغییر: `PUT /api/Users/{id}/password`.

## تست‌ها
تست‌ها مقدارهای مخصوص خودشان را در `EMS.Tests/Support/TestEnvironment.cs` می‌گذارند؛ هیچ مقدار واقعی در آن نیست.
