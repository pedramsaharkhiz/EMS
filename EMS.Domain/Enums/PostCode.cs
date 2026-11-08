namespace EMS.Domain.Enums
{
    /// <summary>
    /// The two posts (سمت) of the system: <c>Line</c> (صف) and <c>Staff</c> (ستاد), as in "line and staff". Each value is also a row of the
    /// <c>Posts</c> table with <c>PostId == (long)value</c> and <c>Code == value.ToString()</c>, so the code can say <c>PostCode.Staff</c> while
    /// administrators still manage the table: they may rename the titles and add more posts (plain labels), but these rows
    /// cannot be deactivated, deleted or given another code. The rows are inserted by the migration and re-created at every start
    /// if someone removed them behind the application's back (see <c>SystemPostsGuard</c>). A new value needs a migration that
    /// inserts its row; a test guards that.
    /// <para>
    /// No business rule depends on either of them for now (ADR-0017): they are labels a user may carry. Do not confuse the post "ستاد"
    /// (<c>Staff</c>) with the supporting department (دپارتمان پشتیبانی، also called ستاد) an equipment can be with: they are unrelated.
    /// </para>
    /// </summary>
    public enum PostCode
    {
        Unknown = 0,   // not a row: "no post" (never stored)
        Line = 1,      // صف
        Staff = 2      // ستاد
    }
}
