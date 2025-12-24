namespace FastBiteGroupMCA.Application.Helper;

public static class Common
{
    public static Guid ToGuidOrDefault(string? input)
    {
        return Guid.TryParse(input, out var guid)
            ? guid
            : Guid.Empty;
    }

    public static Guid ToGuid(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input string is null or empty.");

        return Guid.TryParse(input, out var guid)
            ? guid
            : throw new FormatException($"Cannot parse '{input}' as a valid Guid.");
    }
    public static string GuidToString(Guid guid)
    {
        return guid != Guid.Empty ? guid.ToString() : string.Empty;
    }

    public static bool TryParseGuid(string? input, out Guid guid)
    {
        return Guid.TryParse(input, out guid);
    }
}
