using System.Text.Json;

namespace ContentRecommender.Core.Helpers;

public static class JsonParserHelper
{
    public static bool TryGetProperty(JsonElement element, string path, out JsonElement value)
    {
        value = default;
        if (string.IsNullOrEmpty(path)) return false;
        var parts = path.Split('.');
        var current = element;
        foreach (var part in parts)
        {
            if (!current.TryGetProperty(part, out current))
                return false;
        }
        value = current;
        return true;
    }

    public static string? GetString(JsonElement element, string path)
        => TryGetProperty(element, path, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString()
            : null;

    public static int? GetInt32(JsonElement element, string path)
        => TryGetProperty(element, path, out var val) && val.ValueKind == JsonValueKind.Number && val.TryGetInt32(out var n)
            ? n
            : null;

    public static double? GetDouble(JsonElement element, string path)
        => TryGetProperty(element, path, out var val) && val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var d)
            ? d
            : null;
}