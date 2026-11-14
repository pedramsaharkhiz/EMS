namespace EMS.Tests.Support;

public static class NationalCodeFixture
{
    public static string FromPrefix(string prefix)
    {
        var remainder = Enumerable.Range(0, 9).Sum(i => (prefix[i] - '0') * (10 - i)) % 11;
        return prefix + (remainder < 2 ? remainder : 11 - remainder);
    }

    private static int _next = Random.Shared.Next(1000000, 9000000);
    public static string New() => FromPrefix("00" + Interlocked.Increment(ref _next));
}
