using System.Globalization;
using System.Text.Json;

namespace ContentRecommender.Core.Helpers;

public static class JsonParserHelper
{
    public static bool TryGetProperty(JsonElement element, string path, out JsonElement result)
    {
        result = default;
        if (string.IsNullOrEmpty(path)) return false;

        if (!path.Contains('.'))
            return element.TryGetProperty(path, out result);

        var parts = path.Split('.');
        JsonElement current = element;

        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object ||
                !current.TryGetProperty(part, out var next))
                return false;
            current = next;
        }

        result = current;
        return true;
    }

    public static string? GetString(JsonElement element, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (TryGetProperty(element, path, out var prop))
            return ConvertToJsonString(prop);
        return null;
    }

    public static int? GetInt32(JsonElement element, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (TryGetProperty(element, path, out var prop) && prop.ValueKind == JsonValueKind.Number)
            return prop.TryGetInt32(out var val) ? val : null;
        return null;
    }

    public static double? GetDouble(JsonElement element, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (TryGetProperty(element, path, out var prop) && prop.ValueKind == JsonValueKind.Number)
            return prop.TryGetDouble(out var val) ? val : null;
        return null;
    }
    private static string? ConvertToJsonString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDecimal().ToString(CultureInfo.InvariantCulture),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}