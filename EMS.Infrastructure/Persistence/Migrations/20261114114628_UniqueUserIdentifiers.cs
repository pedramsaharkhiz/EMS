using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueUserIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preflight every user (including inactive ones) before changing any identifiers.
            // The migration transaction holds the user table stable through normalization and index creation.
            migrationBuilder.Sql("""
                DECLARE @whitespace nvarchar(32) = NCHAR(9) + NCHAR(10) + NCHAR(11) + NCHAR(12) + NCHAR(13) + NCHAR(32) + NCHAR(133) + NCHAR(160) + NCHAR(5760) + NCHAR(8192) + NCHAR(8193) + NCHAR(8194) + NCHAR(8195) + NCHAR(8196) + NCHAR(8197) + NCHAR(8198) + NCHAR(8199) + NCHAR(8200) + NCHAR(8201) + NCHAR(8202) + NCHAR(8232) + NCHAR(8233) + NCHAR(8239) + NCHAR(8287) + NCHAR(12288);
                SELECT [Id],
                    TRIM(@whitespace FROM TRANSLATE(CONVERT(nvarchar(32), [PersonnelCode]), N'۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩', N'01234567890123456789')) AS PersonnelCode,
                    NULLIF(TRIM(@whitespace FROM TRANSLATE(CONVERT(nvarchar(10), [NationalCode]), N'۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩', N'01234567890123456789')), N'') AS NationalCode
                INTO #CanonicalUserIdentifiers
                FROM [AspNetUsers] WITH (TABLOCKX, HOLDLOCK);

                IF EXISTS (SELECT 1 FROM #CanonicalUserIdentifiers WHERE PersonnelCode IS NULL OR PersonnelCode = N'')
                    THROW 51001, N'کد پرسنلی خالی در کاربران موجود است. پیش از اجرای مایگریشن داده‌ها را اصلاح کنید.', 1;
                IF EXISTS (SELECT 1 FROM #CanonicalUserIdentifiers GROUP BY PersonnelCode HAVING COUNT(*) > 1)
                    THROW 51002, N'کد پرسنلی تکراری پس از نرمال‌سازی وجود دارد. پیش از اجرای مایگریشن موارد تکراری را اصلاح کنید.', 1;
                IF EXISTS (SELECT 1 FROM #CanonicalUserIdentifiers WHERE NationalCode IS NOT NULL GROUP BY NationalCode HAVING COUNT(*) > 1)
                    THROW 51003, N'کد ملی تکراری پس از نرمال‌سازی وجود دارد. پیش از اجرای مایگریشن موارد تکراری را اصلاح کنید.', 1;
                IF EXISTS (
                    SELECT 1 FROM #CanonicalUserIdentifiers n
                    CROSS APPLY (SELECT (TRY_CONVERT(int, SUBSTRING(n.NationalCode, 1, 1)) * 10 + TRY_CONVERT(int, SUBSTRING(n.NationalCode, 2, 1)) * 9 + TRY_CONVERT(int, SUBSTRING(n.NationalCode, 3, 1)) * 8 + TRY_CONVERT(int, SUBSTRING(n.NationalCode, 4, 1)) * 7 + TRY_CONVERT(int, SUBSTRING(n.NationalCode, 5, 1)) * 6 + TRY_CONVERT(int, SUBSTRING(n.NationalCode, 6, 1)) * 5 + TRY_CONVERT(int, SUBSTRING(n.NationalCode, 7, 1)) * 4 + TRY_CONVERT(int, SUBSTRING(n.NationalCode, 8, 1)) * 3 + TRY_CONVERT(int, SUBSTRING(n.NationalCode, 9, 1)) * 2) % 11 AS Remainder) c
                    WHERE n.NationalCode IS NOT NULL AND (
                        LEN(n.NationalCode) <> 10 OR n.NationalCode COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9]%'
                        OR n.NationalCode = REPLICATE(LEFT(n.NationalCode, 1), 10)
                        OR TRY_CONVERT(int, RIGHT(n.NationalCode, 1)) <> CASE WHEN c.Remainder < 2 THEN c.Remainder ELSE 11 - c.Remainder END))
                    THROW 51004, N'کد ملی نامعتبر در کاربران موجود است. پیش از اجرای مایگریشن داده‌ها را اصلاح کنید.', 1;

                UPDATE u SET PersonnelCode = n.PersonnelCode, NationalCode = n.NationalCode
                FROM [AspNetUsers] u JOIN #CanonicalUserIdentifiers n ON n.Id = u.Id;
                DROP TABLE #CanonicalUserIdentifiers;
                """);

            migrationBuilder.CreateIndex(
                name: "UX_AspNetUsers_NationalCode",
                table: "AspNetUsers",
                column: "NationalCode",
                unique: true,
                filter: "[NationalCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AspNetUsers_PersonnelCode",
                table: "AspNetUsers",
                column: "PersonnelCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_AspNetUsers_NationalCode",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "UX_AspNetUsers_PersonnelCode",
                table: "AspNetUsers");
        }
    }
}
