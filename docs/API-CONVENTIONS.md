# قرارداد پاسخ‌های API

این سند مرجع **تنها قالب پاسخ** همه‌ی endpointهاست. هر پاسخ JSON، چه موفق و چه خطا، یک «پوشش» (envelope) با همان شش کلید است. فرانت یک نوع تعریف می‌کند و روی همه‌ی endpointها استفاده می‌کند.

## ۱. پوشش

```ts
type ApiResponse<T> = {
  success: boolean;               // true فقط برای پاسخ‌های 2xx
  message: string | null;         // متنی که می‌شود به کاربر نشان داد
  data: T | null;                 // خود منبع؛ برای لیست آرایه؛ برای دستورها و خطاها null
  meta: { pagination: Pagination } | null;
  error: { code: string; details: { field: string; message: string }[] } | null;
  traceId: string;                // شناسه‌ی پیگیری درخواست
};

type Pagination = {
  pageNumber: number; pageSize: number; totalCount: number;
  totalPages: number; hasPreviousPage: boolean; hasNextPage: boolean;
};
```

قواعد:

- **هر شش کلید همیشه هستند.** کلیدی که کاربرد ندارد `null` است، نه حذف‌شده.
- **کد HTTP همچنان معنی دارد** (`201`، `404`، `409`، ...). `success` فقط راحتی فرانت است.
- **`traceId`** همان `X-Correlation-ID` درخواست است (اگر کلاینت نفرستد، سرور می‌سازد) و در هدر پاسخ هم هست. هنگام گزارش خطا به پشتیبانی داده می‌شود تا لاگ همان درخواست پیدا شود.
- نام کلیدها همه‌جا **camelCase** است، از جمله `error.details[].field`.
- تاریخ‌ها **ISO 8601 با `Z`** (UTC) هستند، مثلاً `"2026-11-03T10:15:30.123Z"`.
- فیلدی که ممکن است `null` باشد (مثلاً `code` در کاتالوگ‌ها) در پاسخ `null` می‌آید، نه رشته‌ی خالی.

## ۲. پاسخ‌های موفق

| عملیات | status | `message` | `data` | `meta` |
|---|---|---|---|---|
| خواندن یک مورد (`GET /{id}`) | 200 | `null` | منبع | `null` |
| لیست صفحه‌بندی‌شده (`GET`) | 200 | `null` | **آرایه** | `{ pagination }` |
| لیست کوچک بدون صفحه‌بندی (نقش‌های یک کاربر، دسترسی‌های یک نقش) | 200 | `null` | آرایه | `null` |
| ایجاد (`POST`) | **201** + هدر `Location` | «با موفقیت ایجاد شد.» | منبع ساخته‌شده | `null` |
| ویرایش (`PUT /{id}`) | 200 | «با موفقیت ویرایش شد.» | منبع ویرایش‌شده | `null` |
| حذف (`DELETE /{id}`) | **200** | «با موفقیت حذف شد.» | `null` | `null` |
| دستور (نسبت دادن نقش، تنظیم رمز، ابطال توکن، ...) | **200** | متن مخصوص همان عملیات | `null` | `null` |

حذف و دستورها `204` نیستند، چون `204` بدنه ندارد و قالب یکسان را می‌شکند.

### نمونه: یک مورد

`GET /api/Brand/3`

```json
{
  "success": true,
  "message": null,
  "data": { "id": 3, "title": "سامسونگ", "code": "SMS", "isActive": true, "rowVersion": "…" },
  "meta": null,
  "error": null,
  "traceId": "0c9d7f5e1b2a4e6f8a3b9c1d2e4f5a6b"
}
```

### نمونه: لیست

`GET /api/Brand?pageSize=10`

```json
{
  "success": true,
  "message": null,
  "data": [ { "id": 3, "title": "سامسونگ", "code": "SMS", "isActive": true } ],
  "meta": { "pagination": { "pageNumber": 1, "pageSize": 10, "totalCount": 1,
                            "totalPages": 1, "hasPreviousPage": false, "hasNextPage": false } },
  "error": null,
  "traceId": "…"
}
```

### نمونه: حذف و دستور

```json
{ "success": true, "message": "با موفقیت حذف شد.", "data": null, "meta": null, "error": null, "traceId": "…" }
```

## ۳. پاسخ‌های خطا

همه‌ی خطاها (قانون کسب‌وکار، اعتبارسنجی، احراز هویت، دسترسی، مسیر ناموجود، خطای سرور) همین پوشش را دارند، با `success: false`، `data: null`، `meta: null` و `error` پر.

```json
{
  "success": false,
  "message": "برند دیگری با این عنوان وجود دارد.",
  "data": null,
  "meta": null,
  "error": { "code": "duplicate_title", "details": [] },
  "traceId": "…"
}
```

خطای اعتبارسنجی (`400`)، با یک `details` به‌ازای هر مشکل:

```json
{
  "success": false,
  "message": "اطلاعات ارسالی نامعتبر است.",
  "data": null,
  "meta": null,
  "error": {
    "code": "validation_failed",
    "details": [ { "field": "title", "message": "عنوان برند الزامی است." } ]
  },
  "traceId": "…"
}
```

- فرانت روی `error.code` تصمیم می‌گیرد، نه روی متن `message`. فهرست کدها در [ERRORS.md](ERRORS.md) است.
- `error.details[].field` همان نام فیلد در بدنه‌ی درخواست است (camelCase). فیلد تودرتو با نقطه می‌آید (`items[0].name`). برای `message` تنها که به فیلد خاصی ربط ندارد، `details` خالی است.
- نوع محتوا `application/json` است.

### نمونه‌ی ساده‌ی استفاده در فرانت

```ts
const res = await fetch(url, init);
const body: ApiResponse<Brand> = await res.json();

if (body.success) {
  show(body.data);
} else if (body.error?.code === "validation_failed") {
  body.error.details.forEach(d => form.setError(d.field, d.message));
} else {
  toast(body.message);          // و body.traceId را برای پشتیبانی نگه دار
}
```

## ۴. قواعد مشترک همه‌ی endpointها

| قاعده | مقدار |
|---|---|
| نام شناسه‌ی خود منبع | `id` (کلید خارجی‌ها نام کامل دارند: `brandId`، `equipmentTypeId`) |
| ویرایش | `PUT /api/<X>/{id}`؛ شناسه فقط در مسیر است، نه در بدنه |
| `rowVersion` | همه‌ی منبع‌های قابل‌ویرایش (کاتالوگ، کاربر، نقش) در پاسخ `rowVersion` می‌دهند و `PUT` می‌تواند آن را برگرداند. ناسازگاری `409 concurrency_conflict` است. ارسال نکردنش یعنی «آخرین نوشتن برنده است» |
| `isActive` در `PUT` | اگر نیاید `true` فرض می‌شود، پس `PUT` روی رکورد غیرفعال آن را فعال می‌کند. فرم ویرایش باید `isActive` را همیشه بفرستد |
| فیلد متنی در `PUT` | خالی یا ارسال‌نشده یعنی «بدون تغییر» (برای پاک کردن راه‌حل جدا لازم است) |
| `DELETE` | کاتالوگ: غیرفعال‌سازی (با `PUT` و `isActive: true` برمی‌گردد). کاربر: غیرفعال‌سازی. **نقش: حذف دائمی** (چون نام نقش یکتاست)، و با کاربر فعال `409 role_in_use` |
| جستجوی متنی لیست | متن آزاد: `search`. فیلتر فیلد مشخص: نام همان فیلد (`title`، `code`، `isActive`) |
| صفحه‌بندی | `pageNumber` (از 1)، `pageSize` (1 تا 100، پیش‌فرض 10)، `sortBy`، `sortDirection` (`asc`/`desc`) |
| تاریخ | UTC با `Z` |

## ۵. نوشتن endpoint جدید

کنترلر از `BaseApiController` ارث می‌برد و **یکی از این متدها** را برمی‌گرداند. هرگز DTO خام، `IActionResult` یا `NoContent()` برنگردان:

| متد | کاربرد | status |
|---|---|---|
| `Success(data)` | خواندن یک مورد | 200 |
| `Paged(pagedResult)` | لیست صفحه‌بندی‌شده | 200 |
| `CreatedAt(nameof(GetById), new { id = x.Id }, data)` | ایجاد | 201 + `Location` |
| `Updated(data)` | ویرایش | 200 |
| `Deleted()` | حذف | 200 |
| `Done("متن")` | دستور بدون خروجی | 200 |

```csharp
[Permission(Permissions.Brands.Create)]
[HttpPost]
[ProducesResponseType<ApiResponse<BrandDetailDto>>(StatusCodes.Status201Created)]   // فقط برای 201
public async Task<ActionResult<ApiResponse<BrandDetailDto>>> Create([FromBody] BrandCreateDto request, CancellationToken cancellationToken)
{
    var result = await _service.CreateAsync(request, cancellationToken);
    return CreatedAt(nameof(GetById), new { id = result.Id }, result);
}
```

خطا را با `throw` کن (`NotFoundException`، `ConflictException`، `BadRequestException`، ...). `GlobalExceptionMiddleware` آن را به پوشش خطا تبدیل می‌کند. بدنه‌ی خطا را در کنترلر نساز ([ERRORS.md](ERRORS.md)).

## ۶. چطور اجرا می‌شود

- `ApiResponse<T>` در `EMS.Application/Common/Models/ApiResponse.cs` است.
- متدهای کمکی در `EMS.Presentation/Controllers/BaseApiController.cs`.
- خطاها در `EMS.Presentation/Errors/ApiErrors.cs` ساخته می‌شوند. این کلاس از سه مسیر صدا زده می‌شود: `GlobalExceptionMiddleware` (استثناها)، `InvalidModelStateResponseFactory` در `Program.cs` (اعتبارسنجی و JSON خراب) و `UseStatusCodePages` (401/403/404/405 بدون بدنه).
- `ApiResponseConvention` برای هر action پاسخ موفق (از نوع برگشتی) و خطاهای `400`، `401`، `403`، `500` را در OpenAPI اعلام می‌کند. نیازی به `ProducesResponseType` روی هر action نیست، به‌جز `201`.
- `/health/*` و `/openapi`، `/scalar` از پوشش بیرون‌اند (ابزار زیرساخت‌اند، نه API).

## ۷. چه چیزی از پوشش جلوگیری می‌کند که بشکند

- `ApiEnvelopeConventionTests`: اگر action جدیدی چیزی غیر از `Task<ActionResult<ApiResponse<T>>>` برگرداند، شکست می‌خورد.
- `ApiContractSnapshotTests`: پوشش و شکل `data` هر endpoint در `api-contract.baseline.txt` قفل شده است.
- `ErrorContractTests`، `GlobalExceptionMiddlewareTests` و `ApiEnvelopeTests`: شکل خطاها و موفقیت‌ها، `rowVersion` و تاریخ UTC.
