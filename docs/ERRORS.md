# قرارداد خطاهای API

همه‌ی خطاهای API (قانون کسب‌وکار، اعتبارسنجی، احراز هویت، دسترسی، مسیر ناموجود، خطای سرور) **همان پوششی را دارند که پاسخ موفق دارد** (`success`، `message`، `data`، `meta`، `error`، `traceId`، تعریف کامل در [API-CONVENTIONS.md](API-CONVENTIONS.md))، با `success: false` و یک **کد خطای ماشینی پایدار** در `error.code`. کد HTTP نوع خطا را نشان می‌دهد (400، 401، 403، 404، 405، 409، 500).

## قالب

```json
{
  "success": false,
  "message": "برند دیگری با این عنوان وجود دارد.",
  "data": null,
  "meta": null,
  "error": { "code": "duplicate_title", "details": [] },
  "traceId": "0c9d7f5e1b2a4e6f8a3b9c1d2e4f5a6b"
}
```

| فیلد | توضیح |
|---|---|
| `success` | همیشه `false` |
| `message` | متن فارسی خطا که می‌شود به کاربر نشان داد |
| `error.code` | کد پایدار ماشینی. **کلاینت باید روی `code` تصمیم بگیرد، نه روی متن پیام** |
| `error.details` | آرایه‌ی `{ field, message }`. فقط در خطای اعتبارسنجی پر است؛ برای بقیه خالی |
| `traceId` | شناسه‌ی ردگیری (همان هدر `X-Correlation-ID`). به پشتیبانی بدهید تا لاگ همان درخواست پیدا شود |
| `data`، `meta` | همیشه `null` |

نمونه‌ی خطای اعتبارسنجی (`400`):

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

`field` همان نام فیلد در بدنه‌ی درخواست است، با همان املا (camelCase). فیلد تودرتو با نقطه می‌آید (`items[0].name`). نوع محتوا `application/json` است.

## تغییر نسبت به نسخه‌ی قبلی
- قالب `application/problem+json` (RFC 9457) برداشته شد. `type`، `title`، `status`، `detail`، `instance` و `correlationId` دیگر نیستند. `status` همان کد HTTP است، `detail` همان `message`، و `correlationId` همان `traceId`.
- `code` از سطح بالا به `error.code` رفت.
- `errors` (دیکشنری با کلید PascalCase) جایش را `error.details` (آرایه با `field` به‌صورت camelCase) گرفت.
- پاسخ‌های `401` و `403` و `404` (مسیر ناموجود) و `405` همچنان بدنه دارند، با همین قالب.

### عمومی
| code | status | معنی |
|---|---|---|
| `validation_failed` | 400 | اعتبارسنجی ورودی یا JSON نامعتبر (`error.details` را ببینید) |
| `bad_request` | 400 | درخواست نامعتبر |
| `unauthorized` | 401 | توکن نیست یا نامعتبر/منقضی است (یا تغییر دسترسی: refresh بزنید) |
| `forbidden` | 403 | احراز هویت شده، اما permission لازم را ندارد |
| `not_found` | 404 | مسیر یا رکورد وجود ندارد |
| `method_not_allowed` | 405 | متد HTTP برای این مسیر مجاز نیست |
| `conflict` | 409 | تعارض عمومی با وضعیت منبع |
| `unsupported_media_type` | 415 | بدنه JSON نیست؛ هدر `Content-Type` باید `application/json` باشد |
| `not_acceptable` | 406 | قالب پاسخ درخواستی پشتیبانی نمی‌شود |
| `internal_error` | 500 | خطای پیش‌بینی‌نشده‌ی سرور (جزئیاتی نشت نمی‌کند، `traceId` را گزارش کنید) |

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
| `protected_record` | 409 | رکورد سیستمی (مثل وضعیت‌های `EquipmentStatusCode`) قابل غیرفعال‌سازی، حذف یا تغییر کد نیست |
| `reserved_code` | 409 | این کد برای رکورد سیستمی رزرو شده و برای رکورد دیگری قابل استفاده نیست |
| `parent_not_found` | 404 | والد انتخاب‌شده وجود ندارد |
| `parent_inactive` | 400 | والد انتخاب‌شده غیرفعال است |

### تجهیز
| code | status | معنی |
|---|---|---|
| `duplicate_yaas_code` | 409 | تجهیز فعال دیگری با همین یاس کد وجود دارد |
| `status_transition_not_allowed` | 400 | تغییر وضعیت انتخاب‌شده مجاز نیست (فعلاً همه‌ی انتقال‌ها آزاد است) |

### خروجی Excel
| code | status | معنی |
|---|---|---|
| `export_too_large` | 400 | تعداد ردیف‌ها از `Export:MaxRows` بیشتر است؛ فیلتر را محدودتر کنید |
| `export_busy` | 429 | به تعداد `Export:MaxConcurrent` خروجی در حال ساخت است؛ کمی بعد دوباره تلاش کنید |
| `too_many_requests` | 429 | کد عمومی 429 |

### کاربران و نقش‌ها
| code | status | معنی |
|---|---|---|
| `user_name_taken`، `personnel_code_taken`، `national_code_taken` | 409 | مقدار تکراری |
| `user_not_found`، `role_not_found` | 404 | یافت نشد |
| `user_inactive`، `role_inactive` | 400 | کاربر یا نقش غیرفعال است |
| `users_invalid`، `roles_invalid` | 400 | یک یا چند شناسه نامعتبر یا غیرفعال است |
| `department_not_found` | 400 | واحد پشتیبانی انتخاب‌شده وجود ندارد |
| `role_name_required`، `role_name_taken` | 400 / 409 | نام نقش الزامی یا تکراری است |
| `user_already_has_role` | 409 | کاربر نقش فعال دیگری دارد (هر کاربر فقط یک نقش دارد؛ با `PUT /api/Users/{id}/roles` جایگزین کن یا نقش فعلی را بردار) |
| `role_in_use` | 409 | نقش هنوز به کاربر اختصاص دارد |
| `skills_invalid` | 400 | یک یا چند مهارت وجود ندارد یا غیرفعال است |
| `payload_too_large` | 413 | بدنه‌ی درخواست از حد endpoint بزرگ‌تر است |
| `identity_error` | 400 | رد شدن توسط قوانین Identity (مثلاً سیاست رمز) |

## برای توسعه‌دهنده‌ی بک‌اند
- خطای قابل انتظار را با `NotFoundException`، `ConflictException` یا `BadRequestException` بیندازید و کد مناسب را از `ErrorCodes` بدهید: `throw new ConflictException("...", ErrorCodes.DuplicateTitle);`. کنترلر نباید بدنه‌ی خطا را خودش بسازد (`ApiErrors` آن را می‌سازد).
- برای ۴۰۱ از `UnauthorizedAppException` استفاده کنید.
- کد جدید به `ErrorCodes` و همین جدول اضافه شود.

## زبان پیام‌ها
همه‌ی `message` و `error.details[].message` فارسی‌اند، از جمله خطاهای خود فریم‌ورک:
- validatorها `WithMessage` فارسی دارند و برای قاعده‌های بدون پیام، فرهنگ پیش‌فرض FluentValidation روی `fa` است.
- خطاهای ASP.NET Identity با `PersianIdentityErrorDescriber` فارسی می‌شوند (کدها همان می‌مانند).
- خطای اتصال مدل (query نامعتبر، JSON خراب یا خالی، نوع اشتباه) در `ApiErrors.FromModelState` فارسی می‌شود و مسیر JSON و شماره خط/بایت به کلاینت نمی‌رسد. خطای کل بدنه یک‌بار با `field: ""` می‌آید.
- `415` و `406` هم همین پوشش را دارند. تست `PersianMessagesTests` از انگلیسی شدن دوباره جلوگیری می‌کند.

کدهای پیوست‌ها (`attachment_*`) در [ATTACHMENTS.md](ATTACHMENTS.md) آمده‌اند.
