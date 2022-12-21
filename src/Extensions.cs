using System.Diagnostics.CodeAnalysis;

namespace GetMoarFediverse;

public static class Extensions
{
    public static bool IsNullOrEmpty([NotNullWhen(returnValue: false)]this string? s) => string.IsNullOrEmpty(s);

    public static bool HasValue([NotNullWhen(returnValue: true)]this string? s) => !s.IsNullOrEmpty();
}
