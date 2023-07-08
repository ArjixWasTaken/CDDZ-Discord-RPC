using JetBrains.Annotations;

namespace CDInDeeZ.Extensions;

public static class StringExt
{
    public static bool IsNullOrEmpty([CanBeNull] this string s) => s is null or "";
}