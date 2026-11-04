namespace EMS.Domain.Enums
{
    /// <summary>
    /// The three posts (سمت) the business is built on. Each value is also a row of the <c>Posts</c> table with
    /// <c>PostId == (long)value</c> and <c>Code == value.ToString()</c>, so the code can say <c>PostCode.Technician</c> while
    /// administrators still manage the table: they may rename the titles and add more posts (plain labels), but these rows
    /// cannot be deactivated, deleted or given another code. The rows are inserted by the migration and re-created at every start
    /// if someone removed them behind the application's back (see <c>SystemPostsGuard</c>). A new value needs a migration that
    /// inserts its row; a test guards that.
    /// </summary>
    public enum PostCode
    {
        Unknown = 0,       // not a row: "no post" (never stored)
        Technician = 1,    // تکنسین
        UnitManager = 2,   // مسئول واحد
        Registrar = 3      // ثبت‌کننده
    }
}
