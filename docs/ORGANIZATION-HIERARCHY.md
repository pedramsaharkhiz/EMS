# سلسله‌مراتب بانک (`GET /api/OrganizationHierarchy`)

اندپوینت **فقط‌خواندنی** برای ساختار بانک ملت. مجوز: `OrganizationHierarchy.View`. هر درخواست **یک سطح** را برمی‌گرداند (`searchLevel`). جدول‌ها از بیرون پر می‌شوند و API نوشتن ندارد. نگاشت جدول‌ها و دو مسیر شعبه: [BUSINESS-DECISIONS.md](BUSINESS-DECISIONS.md) بخش ۰.

## سطح‌ها

| `searchLevel` | جدول | موجودیت بانکی | والد مستقیم |
|---|---|---|---|
| `zone` | `Zones` | ناحیه | ندارد |
| `organizationManagement` | `OrganizationManagements` | مدیریت شعب | ناحیه |
| `field` | `Fields` | حوزه | مدیریت شعب |
| `organization` | `Organizations` | شعبه | حوزه، یا (بدون حوزه) مدیریت شعب |
| `employee` | `Employees` | **کارمند بانک** | **شعبه** (هر کارمند دقیقاً یک شعبه دارد) |

بالاترین سطح ناحیه است و پایین‌ترین کارمند. دنیای شرکت یاس (`SupportingDepartment`) سطح نیست؛ فقط برای شعبه به‌شکل `supportingDepartmentId/Title` کنارش می‌آید.

## پارامترها

| پارامتر | معنا |
|---|---|
| `searchLevel` | یکی از پنج سطح بالا (الزامی) |
| `parentLevel` + `parentId` | فقط فرزندان یک والد. والد باید **بالاتر** از سطح جستجو باشد. هر سطح بالاتر مجاز است (مثلاً کارمندان یک ناحیه). `parentLevel` بدون `parentId`: پاسخ موفق و خالی |
| `search` | در عنوان و کد (شامل‌بودن) |
| `title` | عنوان (شامل‌بودن). `code`: کد (برابری دقیق) |
| `isActive` | اگر نیاید فعال و غیرفعال هر دو می‌آیند |
| `supportingDepartmentId`، `withoutSupportingDepartment` | فقط برای `organization` و هم‌زمان قابل‌استفاده نیستند |
| `pageNumber`، `pageSize` | `pageSize` از ۱ تا ۱۰۰ (پیش‌فرض ۲۰) |

ترتیب ثابت: عنوان و بعد شناسه. برای **کارمند**: نام خانوادگی، نام، شناسه.

## پاسخ

`{ data: { items: [...] }, meta: { pagination } }`. هر آیتم: `id`، `level`، `title`، `code`، `isActive`، `parent`، `ancestors` (اجداد بالاتر از والد مستقیم، از بالا به پایین)، و برای شعبه `supportingDepartmentId/Title`.

## سطح کارمند (`employee`)

- **`title`** نام کامل (`نام نام‌خانوادگی`) و **`code`** کد پرسنلی است؛ `firstName` و `lastName` هم جدا می‌آیند.
- **`parent`** شعبهٔ کارمند (`level: "organization"`)، و **`ancestors`** حوزه (اگر شعبه حوزه دارد)، مدیریت شعب و ناحیه، از بالا به پایین.
- **`organizationStructure`**: ساختار سازمانی (جایگاه شغلی) کارمند، `{ id, code, title, grade }`. در سطح‌های دیگر `null` است. (فقط خود گره می‌آید، نه زنجیرهٔ والدهایش در درخت.)
- **اطلاعات شخصی حساس برگردانده نمی‌شود:** کد ملی، تاریخ تولد و عکس پروفایل عمداً نیستند.
- `search` در نام، نام خانوادگی و کد پرسنلی؛ `title` در نام کامل؛ `code` کد پرسنلی (دقیق). فیلتر دپارتمان پشتیبانی برای کارمند معتبر نیست (`400`).
- **عملکرد:** با `parentId` (ایندکس خودکار کلید خارجی `Employees.OrganizationId`) سریع است. جست‌وجوی متنی بدون والد روی تعداد زیاد کارمند `LIKE '%…%'` است و ایندکس ندارد.

مثال: کارمندان یک شعبه با ساختارشان

```
GET /api/OrganizationHierarchy?searchLevel=employee&parentLevel=organization&parentId=12
```

```json
{ "id": 7, "level": "employee", "title": "علی احمدی", "firstName": "علی", "lastName": "احمدی", "code": "70012345", "isActive": true,
  "parent": { "id": 12, "level": "organization", "title": "شعبه ...", "code": "..." },
  "organizationStructure": { "id": 3, "code": "EXP", "title": "کارشناس", "grade": 5 },
  "ancestors": [ { "level": "zone", ... }, { "level": "organizationManagement", ... }, { "level": "field", ... } ] }
```
