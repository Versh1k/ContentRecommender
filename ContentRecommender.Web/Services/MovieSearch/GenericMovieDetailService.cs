using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Helpers;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ContentRecommender.Web.Services.MovieSearch;

public class GenericMovieDetailService : IMovieDetailService
{
    private readonly HttpClient _http;
    private readonly MovieApiOptions _options;

    public GenericMovieDetailService(HttpClient http, IOptions<MovieApiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    private ProviderConfig Current => _options.Providers[_options.ActiveProvider];
    private FieldMapping Fm => Current.FieldMapping;

    public async Task<MovieDetailDto?> GetMovieDetailsAsync(string externalId)
    {
        if (!Current.Urls.TryGetValue("GetDetails", out var detailsTemplate))
            return null;

        var url = $"{Current.BaseUrl}{detailsTemplate.Replace("{id}", externalId)}";
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new MovieDetailDto
        {
            ExternalId = externalId,
            Source = _options.ActiveProvider,
            Title = JsonParserHelper.GetString(root, Fm.Title)
                 ?? JsonParserHelper.GetString(root, Fm.TitleFallback)
                 ?? Current.Defaults?.DefaultTitle,
            Description = JsonParserHelper.GetString(root, Fm.Description),
            Year = JsonParserHelper.GetInt32(root, Fm.Year),
            Rating = JsonParserHelper.GetDouble(root, Fm.Rating),
            Genres = ExtractStringArray(root, Fm.Genres, Fm.GenreName),
            DurationMinutes = JsonParserHelper.GetInt32(root, Fm.Duration),
            Director = ExtractFirstString(root, Fm.Directors, Fm.DirectorName),
            Actors = ExtractStringArray(root, Fm.Actors, Fm.ActorName)
                        .Take(Current.Limits?.MaxActors ?? int.MaxValue).ToList(),
            CoverUrl = NormalizePosterUrl(JsonParserHelper.GetString(root, Fm.PosterUrl)),
            Format = DetermineFormat(root),
        };
    }

    public async Task<List<MovieSummaryDto>> GetSimilarMoviesAsync(string externalId, int limit = 9)
    {
        var ss = Current.SimilarSettings;
        if (ss == null || !Current.Urls.TryGetValue("GetSimilar", out var similarTemplate) || string.IsNullOrEmpty(similarTemplate))
        {
            Console.WriteLine($"[Similar] Нет настроек для провайдера {_options.ActiveProvider}");
            return new();
        }

        var url = $"{Current.BaseUrl}{similarTemplate.Replace("{id}", externalId)}";
        Console.WriteLine($"[Similar] Запрос URL: {url}");

        try
        {
            var response = await _http.GetAsync(url);
            Console.WriteLine($"[Similar] Статус ответа: {response.StatusCode}");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Similar] Ошибка ответа: {error}");
                return new();
            }

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Similar] JSON (первые 200 символов): {json.Substring(0, Math.Min(json.Length, 200))}");
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var similar = new List<MovieSummaryDto>();

            if (JsonParserHelper.TryGetProperty(root, ss.RootArrayKey, out var items) && items.ValueKind == JsonValueKind.Array)
            {
                Console.WriteLine($"[Similar] Найдено {items.GetArrayLength()} элементов в '{ss.RootArrayKey}'");
                foreach (var item in items.EnumerateArray().Take(limit))
                {
                    var id = JsonParserHelper.GetString(item, ss.IdKey);
                    Console.WriteLine($"[Similar] ID элемента: {id}");
                    if (string.IsNullOrEmpty(id)) continue;

                    var title = ss.TitleKeys?.Select(k => JsonParserHelper.GetString(item, k))
                                .FirstOrDefault(t => !string.IsNullOrEmpty(t)) ?? Current.Defaults?.DefaultTitle;

                    similar.Add(new MovieSummaryDto
                    {
                        ExternalId = id,
                        Title = title,
                        CoverUrl = NormalizePosterUrl(JsonParserHelper.GetString(item, ss.PosterKey)),
                        Year = null,
                        Rating = null
                    });
                }
            }
            else
            {
                Console.WriteLine($"[Similar] Корневой массив '{ss.RootArrayKey}' не найден или не является массивом");
            }
            Console.WriteLine($"[Similar] Возвращено {similar.Count} похожих фильмов");
            return similar;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Similar] Ошибка для {externalId}: {ex.Message}");
            return new();
        }
    }

    private static List<string> ExtractStringArray(JsonElement root, string arrayPath, string itemField)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(arrayPath) || string.IsNullOrEmpty(itemField)) return result;

        if (JsonParserHelper.TryGetProperty(root, arrayPath, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                var val = JsonParserHelper.GetString(item, itemField);
                if (!string.IsNullOrEmpty(val)) result.Add(val);
            }
        }
        return result;
    }

    private static string? ExtractFirstString(JsonElement root, string arrayPath, string itemField)
    {
        if (string.IsNullOrEmpty(arrayPath) || string.IsNullOrEmpty(itemField)) return null;

        if (JsonParserHelper.TryGetProperty(root, arrayPath, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                var val = JsonParserHelper.GetString(item, itemField);
                if (!string.IsNullOrEmpty(val)) return val;
            }
        }
        return null;
    }

    private string? NormalizePosterUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        var normalized = url;
        foreach (var replacement in Current.PosterUrlNormalization?.Replacements ?? new Dictionary<string, string>())
        {
            normalized = normalized.Replace(replacement.Key, replacement.Value);
        }
        return normalized;
    }

    private ContentFormat DetermineFormat(JsonElement root)
    {
        var type = JsonParserHelper.GetString(root, Fm.Type)?.ToUpper() ?? string.Empty;
        if (string.IsNullOrEmpty(type) || Current.FormatMapping == null)
            return ContentFormat.Movie;

        if (Current.FormatMapping.TryGetValue(type, out var formatStr) &&
            Enum.TryParse(formatStr, out ContentFormat fmt))
        {
            return fmt;
        }
        return ContentFormat.Movie;
    }
}