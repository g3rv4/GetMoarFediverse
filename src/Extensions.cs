namespace GetMoarFediverse;

public static class Extensions
{
    public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);

    public static bool HasValue(this string s) => !s.IsNullOrEmpty();
}
