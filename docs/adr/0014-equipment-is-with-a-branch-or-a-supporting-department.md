# ADR-0014: تجهیز دقیقاً یا نزد یک شعبه است یا نزد یک ستاد؛ کاتالوگ «نوع مکان» حذف شد

- **وضعیت:** پذیرفته‌شده
- **ثبت‌شده در:** ۲۰۲۶-۱۱-۰۸

## زمینه
هر تجهیز سه ستون مکان داشت: `CurrentEquipmentLocationTypeId` (الزامی) و `CurrentOrganizationId` و `CurrentSupportingDepartmentId` (اختیاری). هیچ قیدی بین این‌ها نبود، پس هر ترکیبی ممکن بود (هر دو، هیچ‌کدام) و برای اینکه بدانیم تجهیز «دست شعبه است یا دست ستاد» باید نوع مکان را می‌خواندیم، که خودش از ستون‌های دیگر مستقل بود. جدول `EquipmentLocationTypes` هیچ منطقی نداشت و فقط یک برچسب بود. پروژه در فاز توسعهٔ اولیه است و هیچ داده‌ای در دیتابیس ندارد.

## تصمیم
- **قید دیتابیس:** دقیقاً یکی از `CurrentOrganizationId` و `CurrentSupportingDepartmentId` مقدار دارد (`CK_Equipments_Location_ExactlyOne`). همین قید روی `EquipmentLocationHistories` هم هست (`CK_EquipmentLocationHistories_Location_ExactlyOne`).
- **API:** ثبت، ثبت گروهی، ثبت با فایل و `move` دقیقاً یکی از دو فیلد را می‌گیرند (اعتبارسنجی فارسی `validation_failed` روی `currentOrganizationId`، و لایهٔ دوم سرویس با `location_holder_invalid`).
- **کاتالوگ حذف شد:** جدول `EquipmentLocationTypes`، دو ستون کلید خارجی، endpointهای `/api/EquipmentLocationType`، مجوزهای `EquipmentLocationTypes.*` (در `Permissions.Retired`)، فیلد در ثبت و `move`، فیلتر `locationTypeId`، مرتب‌سازی و ستون Excel.
- **جایگزین نوع مکان:** فیلد محاسبه‌ای `locationKind` (enum `EquipmentLocationKind`: ۱ = شعبه، ۲ = ستاد) با `locationKindTitle`، مشتق از اینکه کدام ستون پر است. ذخیره نمی‌شود. فیلتر `locationKind` و ستون Excel «محل نگهداری».
- **تکنسین مستقل ماند:** «نزد تکنسین» با `technicianUserId` پر پوشش داده می‌شود.
- **بدون مهاجرت داده:** مایگریشن ستون‌ها و جدول را حذف می‌کند و چیزی را تبدیل نمی‌کند، چون داده‌ای نبود.

## گزینه‌های بررسی‌شده
- **«حداکثر یکی» (تجهیز بدون محل هم مجاز):** رد شد؛ صاحب محصول «دقیقاً یکی» خواست.
- **نگه‌داشتن موقت جدول با ستون اختیاری و حذف بعداً (دو مرحله):** رد شد؛ فرانت هم‌زمان با بک عوض می‌شود و داده‌ای برای محافظت نیست.
- **نگه‌داشتن همیشگی جدول:** رد شد؛ پیچیدگی‌ای که می‌خواستیم کم کنیم می‌ماند.

## پیامدها
- تشخیص «دست شعبه یا ستاد» فقط از یک فیلد و هر تجهیز همیشه یک محل مشخص دارد.
- **تغییر شکن برای فرانت:** بدنهٔ ثبت و `move` فیلد `currentEquipmentLocationTypeId` را نمی‌گیرد و باید یکی از شعبه و دپارتمان را بفرستد؛ پاسخ‌ها `currentEquipmentLocationTypeId/Title` را ندارند و به‌جایش `locationKind/locationKindTitle` می‌دهند. مسیرهای کاتالوگ نوع مکان `404` می‌شوند.
- تجهیزِ ستادی حوزه، مدیریت و ناحیه ندارد (آن‌ها از شعبه می‌آیند) و تجهیزِ شعبه‌ای شهر و استان ندارد (از دپارتمان). این رفتار پیش‌تر هم بود و حالا کاملاً دو‌حالتی است.
- «همهٔ تجهیز زیر مسئولیت ستاد X» یعنی تجهیز شعبه‌های ستاد X (`supportingDepartmentId`) **یا** تجهیز خودِ ستاد X (`currentSupportingDepartmentId`)؛ فعلاً دو فیلتر جدا هستند.
- `Down` مایگریشن ساختار را برمی‌گرداند، نه داده را، و روی جدولی که ردیف دارد شکست می‌خورد (ستون الزامی بدون مقدار).
- اجرای مایگریشن روی دیتابیسی که داده دارد پیش از هر چیز به پشتیبان و بررسی ردیف‌های ناقض قید نیاز دارد (سرآیند `docs/sql/EquipmentLocationExactlyOneHolder.sql`).

## ارجاع
`docs/BUSINESS-DECISIONS.md` (بخش ۳.۹)، `docs/EQUIPMENT.md`، `EMS.Infrastructure/Persistence/Migrations/*EquipmentLocationExactlyOneHolder.cs`
