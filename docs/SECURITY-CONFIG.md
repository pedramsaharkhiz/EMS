# تنظیمات امنیتی: headerها و کوکی‌ها

همه‌ی تنظیمات این سند در بخش `Security` فایل `appsettings.json` هستند. مقدار هر محیط را می‌توان در `appsettings.<Environment>.json` بازنویسی کرد (برای Development همین‌طور انجام شده است).

## Security headers (`Security:Headers`)

middleware `SecurityHeadersMiddleware` اولین middleware برنامه است، پس headerها روی **همه‌ی پاسخ‌ها** می‌نشینند: موفق، خطا (۴۰۱ و ۴۰۳ و ۴۰۴ و ۵۰۰) و پاسخ preflight مربوط به CORS. تغییر در appsettings **بدون راه‌اندازی مجدد** اعمال می‌شود.

| کلید | کار |
|---|---|
| `Enabled` | کلید اصلی. `false` یعنی هیچ headerی اضافه یا حذف نشود (در لاگ startup هشدار می‌دهد). |
| `RemoveServerHeader` | حذف `Server: Kestrel`. فقط روی Kestrel اثر دارد (پشت IIS یا proxy، همان‌ها header خودشان را اضافه می‌کنند). |
| `Hsts` | تنظیمات `Strict-Transport-Security` (پایین‌تر). |
| `Values` | نام header ← مقدار. هر headerی را می‌توان اضافه کرد. مقدار خالی (`""`) آن header را در آن محیط غیرفعال می‌کند. |
| `Remove` | headerهایی که از همه‌ی پاسخ‌ها حذف شوند (مثلاً `X-Powered-By`). |

قواعد:
- headerی که خود برنامه یا framework از قبل گذاشته باشد **بازنویسی نمی‌شود** (مثلاً `Cache-Control` مربوط به `/health`).
- نام یا مقدار نامعتبر (فاصله، `:`، خط جدید) در startup رد می‌شود و برنامه بالا نمی‌آید، تا بی‌صدا بدون حفاظت اجرا نشود.

### headerهای پیش‌فرض

| Header | مقدار | چرا |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | جلوگیری از حدس زدن نوع محتوا توسط مرورگر |
| `X-Frame-Options` | `DENY` | جلوگیری از قرار گرفتن در iframe (clickjacking)، برای مرورگرهای قدیمی |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` | یک API هیچ منبعی را load نمی‌کند و نباید در frame بیاید |
| `Referrer-Policy` | `no-referrer` | آدرس‌ها به سایت دیگر نشت نکنند |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=(), usb=()` | غیرفعال کردن قابلیت‌های مرورگر |
| `Cross-Origin-Opener-Policy` | `same-origin` | جداسازی پنجره‌ها |
| `Cross-Origin-Resource-Policy` | `same-site` | منابع API فقط توسط همان site قابل embed باشند |
| `X-Permitted-Cross-Domain-Policies` | `none` | جلوگیری از سیاست‌های Flash/PDF |
| `Cache-Control`، `Pragma` | `no-store`، `no-cache` | پاسخ‌ها (توکن‌ها، داده‌ی شخصی) توسط مرورگر یا proxy cache نشوند |

اگر فرانت‌اند شما روی همان domain/site نیست و منابعی از API (مثلاً تصویر پیوست) را با تگ `<img>` embed می‌کند، `Cross-Origin-Resource-Policy` را به `cross-origin` تغییر دهید.

### HSTS (`Security:Headers:Hsts`)

`Strict-Transport-Security` فقط روی درخواست‌های **HTTPS** ارسال می‌شود و هرگز برای `localhost`، `127.0.0.1` و `[::1]` (و `ExcludedHosts` اضافه). تنظیمات: `Enabled`، `MaxAgeDays` (پیش‌فرض ۳۶۵)، `IncludeSubDomains`، `Preload`.

- `Preload` را فقط وقتی روشن کنید که مطمئنید همه‌ی subdomainها برای مدت طولانی HTTPS خواهند ماند (برگشتش سخت است).
- **پشت reverse proxy که TLS را خودش تمام می‌کند**، برنامه درخواست را HTTP می‌بیند و HSTS نمی‌فرستد. در این حالت یا HSTS را روی proxy تنظیم کنید، یا `UseForwardedHeaders` را پیکربندی کنید تا برنامه HTTPS را تشخیص دهد.

### Development

در `appsettings.Development.json` فقط سه چیز عمداً شل شده است، چون توسعه روی `http://localhost` انجام می‌شود:
- `Hsts.Enabled = false`
- `Content-Security-Policy = ""` (رابط مستندات Scalar اسکریپت load می‌کند)
- `Cookies.SecurePolicy = SameAsRequest`

### IIS و `Server` / `X-Powered-By`

اگر برنامه روی IIS اجرا می‌شود، IIS خودش `Server` و `X-Powered-By` را اضافه می‌کند. `X-Powered-By` با `Remove` حذف می‌شود، اما `Server` را باید در IIS (`removeServerHeader="true"` در `requestFiltering` فایل `web.config`) بردارید.

## کوکی‌ها (`Security:Cookies`)

### نتیجه‌ی بررسی

- **برنامه هیچ کوکی‌ای صادر نمی‌کند.** احراز هویت با توکن Bearer در هدر `Authorization` است. ورود با `CheckPasswordSignInAsync` انجام می‌شود که کوکی نمی‌سازد، و هیچ `SignInAsync`، Session، TempData یا Antiforgery در کد نیست. این با تست خودکار بررسی می‌شود (ورود موفق و ناموفق، refresh، ۴۰۱، health: هیچ‌کدام `Set-Cookie` ندارند).
- توکن فقط از هدر `Authorization` خوانده می‌شود، نه از query string و نه از کوکی. پس نه در لاگ سرور و تاریخچه‌ی مرورگر می‌ماند و نه حمله‌ی CSRF روی آن ممکن است.
- تنها سطح مرتبط با کوکی، schemeهای کوکی ASP.NET Core Identity است که `AddIdentity` ثبت می‌کند و استفاده نمی‌شوند. مقدار پیش‌فرض کتابخانه برای آن‌ها ضعیف است (`SecurePolicy=SameAsRequest`، `SameSite=Lax`، ۱۴ روز با sliding). اگر روزی کسی `PasswordSignInAsync` اضافه می‌کرد، کوکی با همین مقادیر ضعیف ساخته می‌شد.

### تنظیمات

این مقادیر روی هر چهار کوکی Identity اعمال می‌شوند (Application، External، TwoFactorRememberMe، TwoFactorUserId):

| کلید | پیش‌فرض | توضیح |
|---|---|---|
| `HttpOnly` | `true` | از JavaScript خوانده نشود (محافظت در برابر XSS) |
| `SecurePolicy` | `Always` | فقط روی HTTPS ارسال شود. مقادیر: `Always`، `SameAsRequest`، `None` |
| `SameSite` | `Strict` | روی درخواست‌های cross-site ارسال نشود (محافظت در برابر CSRF). مقادیر: `Strict`، `Lax`، `None`، `Unspecified` |
| `ExpireMinutes` | `30` | عمر کوکی ورود (Application) |
| `SlidingExpiration` | `false` | با فعالیت تمدید نشود |

کوکی Application به‌جای redirect به صفحه‌ی ورود، `401` یا `403` برمی‌گرداند.

> اگر روزی ورود با ارائه‌دهنده‌ی خارجی (OAuth) اضافه شود، کوکی External به `SameSite=Lax` نیاز دارد، چون بازگشت از ارائه‌دهنده یک redirect cross-site است.

### توصیه‌ها (تغییر نداده‌ام)

1. **محل نگهداری refresh token در فرانت‌اند.** API توکن‌ها را در بدنه‌ی پاسخ می‌دهد. اگر فرانت آن‌ها را در `localStorage` نگه دارد، هر XSS می‌تواند refresh token را بدزدد. امن‌ترین الگو: refresh token در کوکی `HttpOnly; Secure; SameSite=Strict` و access token فقط در حافظه. این تغییر قرارداد API است، پس بدون هماهنگی با فرانت انجام نشد.
2. در `Program.cs` CORS با `AllowCredentials()` پیکربندی شده، ولی چون کوکی استفاده نمی‌شود نیازی به آن نیست و فقط سطح حمله را بزرگ‌تر می‌کند. فقط اگر کوکی به معماری اضافه شود لازم می‌شود.

## تست‌ها

`SecurityHeadersMiddlewareTests`، `SecurityConfigurationFilesTests` و `SecurityConfigurationTests` در `EMS.Tests`: headerها روی پاسخ موفق، خطا، مسیر ناموجود و preflight؛ HSTS (فقط HTTPS، نه loopback)؛ بازنویسی نشدن headerهای برنامه؛ غیرفعال‌سازی با مقدار خالی؛ اعتبارسنجی؛ فایل‌های واقعی appsettings هر محیط؛ حذف `Server` روی Kestrel واقعی؛ نبودن هرگونه `Set-Cookie`؛ و مقادیر مؤثر کوکی‌های Identity.

> محدودیت: رفتار پشت IIS یا reverse proxy واقعی، و نمایش واقعی در مرورگر، تست نشده است.


## محدودیت نرخ ورود و رفرش (`Security:RateLimiting`)

دو endpoint بدون توکن (`POST /api/Identity/Login` و `/RefreshToken`) برای هر آدرس کلاینت سقف دارند، با پنجره‌ی ثابت:

| تنظیم | پیش‌فرض |
|---|---|
| `Enabled` | `true` (سوییچ کلی) |
| `Login:PermitLimit` / `Login:WindowSeconds` | ۱۰ درخواست در ۶۰ ثانیه |
| `Refresh:PermitLimit` / `Refresh:WindowSeconds` | ۳۰ درخواست در ۶۰ ثانیه |

- بعد از سقف: `429 too_many_requests` با پیام فارسی و هدر `Retry-After`. هدرهای امنیتی روی همین پاسخ هم هست و رویداد در لاگ امنیتی (`Rate limit reached`) ثبت می‌شود.
- کلید محدودیت **آدرس IP** است. برای IPv6 ۶۴ بیت اول (کاربر معمولاً کل /64 را دارد). قفل حساب (۵ تلاش ناموفق، ۱۵ دقیقه) جداست و برای هجوم از چند آدرس روی یک نام کاربری می‌ماند.
- **پشت پراکسی معکوس** باید `ForwardedHeaders` (پایین) روشن باشد، وگرنه همه‌ی کاربران یک آدرس (پراکسی) به نظر می‌رسند و هم‌زمان سقف را می‌خورند.
- سقف‌ها در زمان درخواست خوانده می‌شوند.

## آدرس واقعی کلاینت پشت پراکسی (`Security:ForwardedHeaders`)

| تنظیم | معنی |
|---|---|
| `Enabled` | پیش‌فرض `false`. فقط وقتی برنامه واقعاً پشت پراکسی (IIS، nginx، load balancer) است روشن کن |
| `ForwardLimit` | تعداد پراکسی‌های پشت‌سرهم (پیش‌فرض ۱) |
| `KnownProxies` | آدرس پراکسی‌ها، مثل `["10.0.0.5"]` |
| `KnownNetworks` | محدوده‌ها به شکل CIDR، مثل `["10.0.0.0/24"]` |

- فقط درخواست‌هایی که از پراکسی فهرست‌شده می‌آیند به هدرهای `X-Forwarded-For` و `X-Forwarded-Proto` اعتماد می‌شود؛ هر کسی که مستقیم به برنامه برسد می‌تواند این هدرها را جعل کند.
- اگر `Enabled=true` و هر دو فهرست خالی باشند، فقط همان سیستم (`127.0.0.1` و `::1`) مورد اعتماد است (برای nginx روی همان سرور).
- مقدار نامعتبر در فهرست‌ها برنامه را هنگام شروع متوقف می‌کند.
- اثر: آدرس درست در لاگ‌ها، در `CreatedByIp` توکن‌ها و در محدودیت نرخ.

## زمان پاسخ ورود
برای نام کاربری ناموجود یا غیرفعال هم یک بررسی رمز (هش) ساختگی انجام می‌شود تا زمان پاسخ، وجود نام کاربری را لو ندهد.
