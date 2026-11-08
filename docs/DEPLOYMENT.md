# استقرار: آدرس‌ها، CORS و HTTPS

## origin مجاز مرورگر (`Cors:AllowedOrigins`)

مرورگر فقط به API‌ای اجازهٔ پاسخ به صفحه می‌دهد که origin آن صفحه در لیست باشد. لیست از تنظیمات خوانده می‌شود (بدون ساخت دوباره):

- `appsettings.json`: خالی (هیچ origin مجاز نیست).
- `appsettings.Development.json`: آدرس‌های `localhost` توسعه.
- `appsettings.Production.json`: آدرس UI واقعی (فعلاً `http://192.168.200.62` و `https://192.168.200.62`).
- یا متغیر محیطی: `Cors__AllowedOrigins__0=http://192.168.200.62`، `Cors__AllowedOrigins__1=...`

> آرایه‌ها در تنظیمات **ایندکس‌به‌ایندکس ادغام می‌شوند**؛ برای همین فایل پایه خالی است و هر محیط لیست کامل خودش را دارد، وگرنه عنصرهای اضافهٔ فایل پایه می‌ماندند.

**قاعدهٔ نوشتن origin:** دقیقاً همان چیزی که مرورگر در هدر `Origin` می‌فرستد: `scheme://host[:port]`، بدون `/` آخر و بدون مسیر.
- پورت پیش‌فرض (۸۰ برای http، ۴۴۳ برای https) **نوشته نمی‌شود**: `http://192.168.200.62` نه `http://192.168.200.62:80`.
- `http` و `https` دو origin جدا هستند. `localhost` و آی‌پی هم جدا هستند.
- `*` پذیرفته نیست (API اعتبارنامه می‌پذیرد).
- origin نامعتبر مانع بالا آمدن برنامه است (پیام: `Cors configuration is invalid`)؛ لیست خالی فقط هشدار لاگ می‌دهد و هیچ origin مجاز نیست.

برای پیدا کردن مقدار درست: در مرورگر F12 ← Network ← یک درخواست به API ← Request Headers ← `Origin`.

## ریدایرکت http به https (`Https:RedirectEnabled`)

وقتی برنامه یک پورت HTTPS بشناسد (مثلاً binding ۴۴۳ در IIS)، با `true` هر درخواست `http` را با ۳۰۷ به `https` می‌فرستد. preflight مرورگر ریدایرکت را نمی‌پذیرد و خطای CORS می‌دهد، پس تا وقتی UI و API هر دو روی `http` هستند، `false` بگذار (پیش‌فرض Production همین است). فایل پایه `true` است. وقتی هر دو روی https کار کردند، `true` کن.

## HSTS

`Strict-Transport-Security` فقط روی پاسخ‌های HTTPS و برای host‌های غیر loopback فرستاده می‌شود و مرورگر آن را برای آی‌پی اعمال نمی‌کند. فعلاً در Production روشن مانده است؛ اگر با نام دامنه روی https رفتی، مرورگر آن دامنه را یک سال به https قفل می‌کند (جزئیات: [SECURITY-CONFIG.md](SECURITY-CONFIG.md)).

## هم‌خوانی UI و API

- صفحه و API هر دو `http` یا هر دو `https`. صفحهٔ https که به API با http درخواست بزند «Mixed Content» می‌گیرد و مرورگر درخواست را اصلاً نمی‌فرستد.
- آدرس API در UI نباید `localhost` باشد (روی کامپیوتر کاربر، `localhost` خود او است).
- بک باید روی کارت شبکه‌ای که کاربران می‌بینند گوش بدهد و پورتش در فایروال باز باشد.

## IIS

- روی سرور **ASP.NET Core Hosting Bundle** (نسخهٔ ۱۰) نصب باشد. `dotnet publish` فایل `web.config` را می‌سازد.
- binding با `https` بدون گواهی ساخته نمی‌شود. گواهی خودامضا برای آی‌پی: `New-SelfSignedCertificate -DnsName "192.168.200.62" -CertStoreLocation Cert:\LocalMachine\My` (مرورگر هشدار می‌دهد و باید یک بار پذیرفته شود). گواهی معتبر از CA سازمان یا Let's Encrypt.
- مایگریشن‌ها خودکار اجرا نمی‌شوند ([DATABASE-MIGRATIONS.md](DATABASE-MIGRATIONS.md)).
