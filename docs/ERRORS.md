# قرارداد خطاهای API

همه‌ی خطاهای API (قانون کسب‌وکار، اعتبارسنجی، احراز هویت، دسترسی، مسیر ناموجود، خطای سرور) **یک قالب یکسان** دارند: سند «problem details» استاندارد RFC 9457/7807 با نوع محتوای `application/problem+json`، به‌علاوه‌ی یک **کد خطای ماشینی پایدار**.

## قالب

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "برند دیگری با این عنوان وجود دارد.",
  "instance": "/api/Brand",

  "code": "duplicate_title",
  "message": "برند دیگری با این عنوان وجود دارد.",
  "correlationId": "0c9d7f5e1b2a4e6f8a3b9c1d2e4f5a6b",
  "traceId": "0c9d7f5e1b2a4e6f8a3b9c1d2e4f5a6b"
}
```

| فیلد | توضیح |
|---|---|
| `type`، `title`، `status`، `detail`، `instance` | اعضای استاندارد RFC. `detail` متن فارسی خطا و `instance` مسیر درخواست است |
| **`code`** | کد پایدار ماشینی. **کلاینت باید روی `code` تصمیم بگیرد، نه روی متن پیام** |
| `message` | همان متن `detail`. برای سازگاری با کلاینت‌های قدیمی نگه داشته شده است |
| `correlationId`، `traceId` | شناسه‌ی ردگیری (همان هدر `X-Correlation-ID`). به پشتیبانی بدهید تا لاگ همان درخواست پیدا شود |
| `errors` | فقط در خطای اعتبارسنجی: دیکشنری `نام فیلد ← آرایه‌ای از پیام‌ها` |

نمونه‌ی خطای اعتبارسنجی (`400`):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "اطلاعات ارسالی نامعتبر است.",
  "instance": "/api/Brand",
  "code": "validation_failed",
  "message": "اطلاعات ارسالی نامعتبر است.",
  "correlationId": "…", "traceId": "…",
  "errors": { "Title": ["عنوان برند الزامی است."] }
}
```

## سازگاری با نسخه‌ی قبلی
- `message` و `correlationId` که قبلاً در بعضی خطاها بودند، حالا در **همه** خطاها هستند.
- `errors` برای اعتبارسنجی همان دیکشنری قبلی (`نام فیلد ← پیام‌ها`) است.
- نوع محتوا از `application/json` به `application/problem+json` تغییر کرده. اگر کلاینت شما فقط به نوع `application/json` وابسته است، آن را به `+json` تعمیم دهید.
- پاسخ‌های `401` و `403` و `404` (مسیر ناموجود) و `405` که قبلاً **بدنه‌ی خالی** داشتند، حالا بدنه‌ی بالا را دارند.

## کدهای خطا

کدهای جدید ممکن است اضافه شوند. کدهای موجود هرگز تغییر نام نمی‌دهند.

### عمومی
| code | status | معنی |
|---|---|---|
| `validation_failed` | 400 | اعتبارسنجی ورودی یا JSON نامعتبر (`errors` را ببینید) |
| `bad_request` | 400 | درخواست نامعتبر |
| `unauthorized` | 401 | توکن نیست یا نامعتبر/منقضی است (یا تغییر دسترسی: refresh بزنید) |
| `forbidden` | 403 | احراز هویت شده، اما permission لازم را ندارد |
| `not_found` | 404 | مسیر یا رکورد وجود ندارد |
| `method_not_allowed` | 405 | متد HTTP برای این مسیر مجاز نیست |
| `conflict` | 409 | تعارض عمومی با وضعیت منبع |
| `internal_error` | 500 | خطای پیش‌بینی‌نشده‌ی سرور (جزئیاتی نشت نمی‌کند، `correlationId` را گزارش کنید) |

### احراز هویت
| code | status | معنی |
|---|---|---|
| `invalid_credentials` | 401 | نام کاربری یا رمز اشتباه، حساب غیرفعال یا قفل‌شده (عمداً یکسان) |
| `invalid_refresh_token` | 401 | refresh token نامعتبر، منقضی، ابطال‌شده یا هم‌زمان مصرف‌شده |
| `invalid_user` | 401 | کاربر صاحب توکن وجود ندارد یا غیرفعال است |

### یکپارچگی داده
| code | status | معنی |
|---|---|---|
| `concurrency_conflict` | 409 | رکورد در این فاصله توسط کاربر دیگری تغییر کرده (`rowVersion` قدیمی). دوباره بخوانید |
| `invalid_row_version` | 400 | مقدار `rowVersion` قابل‌خواندن نیست |
| `unique_violation` | 409 | نقض یکتایی در دیتابیس |
| `foreign_key_violation` | 409 | وابستگی یا ارجاع نامعتبر |

### داده‌های پایه
| code | status | معنی |
|---|---|---|
| `duplicate_title` | 409 | عنوان تکراری (در بین رکوردهای فعال) |
| `has_active_dependents` | 409 | رکورد وابسته‌ی فعال دارد و غیرفعال نمی‌شود |
| `parent_not_found` | 404 | والد انتخاب‌شده وجود ندارد |
| `parent_inactive` | 400 | والد انتخاب‌شده غیرفعال است |

### کاربران و نقش‌ها
| code | status | معنی |
|---|---|---|
| `user_name_taken`، `personnel_code_taken`، `national_code_taken` | 409 | مقدار تکراری |
| `user_not_found`، `role_not_found` | 404 | یافت نشد |
| `user_inactive`، `role_inactive` | 400 | کاربر یا نقش غیرفعال است |
| `users_invalid`، `roles_invalid` | 400 | یک یا چند شناسه نامعتبر یا غیرفعال است |
| `department_not_found` | 400 | واحد پشتیبانی انتخاب‌شده وجود ندارد |
| `role_name_required`، `role_name_taken` | 400 / 409 | نام نقش الزامی یا تکراری است |
| `role_in_use` | 409 | نقش هنوز به کاربر اختصاص دارد |
| `identity_error` | 400 | رد شدن توسط قوانین Identity (مثلاً سیاست رمز) |

## برای توسعه‌دهنده‌ی بک‌اند
- خطای قابل انتظار را با `NotFoundException`، `ConflictException` یا `BadRequestException` بیندازید و کد مناسب را از `ErrorCodes` بدهید: `throw new ConflictException("...", ErrorCodes.DuplicateTitle);`. کنترلر نباید بدنه‌ی خطا را خودش بسازد.
- برای ۴۰۱ از `UnauthorizedAppException` استفاده کنید.
- کد جدید به `ErrorCodes` و همین جدول اضافه شود.
