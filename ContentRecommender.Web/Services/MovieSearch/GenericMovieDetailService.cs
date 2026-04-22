using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ContentRecommender.Web.Services.MovieSearch;

public class GenericMovieDetailService : IMovieDetailService
{
    private readonly HttpClient _http;
    private readonly MovieApiOptions _options;

    public GenericMovieDetailService(HttpClient http,
                                     IOptions<MovieApiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    private ProviderConfig Current => _options.Providers[_options.ActiveProvider];
    private FieldMapping Fm => Current.FieldMapping;

    public async Task<MovieDetailDto?> GetMovieDetailsAsync(string externalId)
    {
        Console.WriteLine($"[MovieDetail] Fetching details for ExternalId: '{externalId}'");
        var url = $"{Current.BaseUrl}/{externalId}";
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[MovieDetail] API error {response.StatusCode} for ID '{externalId}'");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new MovieDetailDto
        {
            ExternalId = externalId,
            Source = _options.ActiveProvider,
            Title = GetString(root, Fm.Title) ?? GetString(root, Fm.TitleFallback) ?? "Без названия",
            Description = GetString(root, Fm.Description),
            Year = GetInt32(root, Fm.Year),
            Rating = GetDouble(root, Fm.Rating),
            Genres = GetGenres(root),
            DurationMinutes = GetInt32(root, Fm.Duration),
            Director = GetDirector(root),
            Actors = GetActors(root).Take(10).ToList(),
            CoverUrl = GetPosterUrl(root),
            Format = DetermineFormat(root),
            Trailers = await GetTrailersAsync(externalId)
        };
    }

    public async Task<List<VideoDto>> GetTrailersAsync(string externalId)
    {
        var url = $"{Current.BaseUrl}/{externalId}/videos";
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new();

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<KinopoiskVideosDto>(json);
        return data?.items
            ?.Where(v => v.site?.Contains("youtube", StringComparison.OrdinalIgnoreCase) == true)
            ?.Select(v => new VideoDto
            {
                Title = v.name ?? "Трейлер",
                YouTubeId = ExtractYouTubeId(v.url)
            })
            ?.Take(3)
            ?.ToList() ?? new();
    }

    public async Task<List<MovieSummaryDto>> GetSimilarMoviesAsync(string externalId, int limit = 6)
    {
        var url = $"{Current.BaseUrl}/{externalId}/similar";
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new();

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<KinopoiskSimilarDto>(json);
        return data?.films?.Take(limit).Select(f => new MovieSummaryDto
        {
            ExternalId = f.filmId.ToString(),
            Title = f.nameRu ?? f.nameEn ?? "Без названия",
            CoverUrl = f.posterUrl?.Replace("/300x450/", "/200x300/"),
            Year = f.year,
            Rating = f.ratingKinopoisk,
            Format = f.type == "TV_SERIES" ? ContentFormat.Series : ContentFormat.Movie
        }).ToList() ?? new();
    }

    // ---------- Вспомогательные методы ----------
    private static bool TryGetProperty(JsonElement element, string path, out JsonElement value)
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

    private static string? GetString(JsonElement element, string path)
        => TryGetProperty(element, path, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static int? GetInt32(JsonElement element, string path)
        => TryGetProperty(element, path, out var val) && val.ValueKind == JsonValueKind.Number && val.TryGetInt32(out var n) ? n : null;

    private static double? GetDouble(JsonElement element, string path)
        => TryGetProperty(element, path, out var val) && val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var d) ? d : null;

    private List<string> GetGenres(JsonElement root)
    {
        var genres = new List<string>();
        if (TryGetProperty(root, Fm.Genres, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var g in arr.EnumerateArray())
                if (TryGetProperty(g, Fm.GenreName, out var name) && name.ValueKind == JsonValueKind.String)
                    genres.Add(name.GetString()!);
        return genres;
    }

    private string? GetDirector(JsonElement root)
    {
        if (TryGetProperty(root, Fm.Directors, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var d in arr.EnumerateArray())
                if (TryGetProperty(d, Fm.DirectorName, out var name) && name.ValueKind == JsonValueKind.String)
                    return name.GetString();
        return null;
    }

    private IEnumerable<string> GetActors(JsonElement root)
    {
        if (TryGetProperty(root, Fm.Actors, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var a in arr.EnumerateArray())
                if (TryGetProperty(a, Fm.ActorName, out var name) && name.ValueKind == JsonValueKind.String)
                    yield return name.GetString()!;
    }

    private string? GetPosterUrl(JsonElement root)
    {
        var poster = GetString(root, Fm.PosterUrl);
        if (!string.IsNullOrEmpty(poster))
            poster = poster.Replace("/300x450/", "/700x1000/");
        return poster;
    }

    private ContentFormat DetermineFormat(JsonElement root)
    {
        var type = GetString(root, Fm.Type) ?? "";
        if (type.Contains("TV_SERIES", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("MINI_SERIES", StringComparison.OrdinalIgnoreCase))
            return ContentFormat.Series;
        var genres = GetGenres(root);
        if (genres.Any(g => g.Contains("аниме") || g.Contains("мульт")))
            return ContentFormat.Cartoon;
        return ContentFormat.Movie;
    }

    private string ExtractYouTubeId(string? url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        var match = Regex.Match(url, @"(?:v=|\/)([a-zA-Z0-9_-]{11})");
        return match.Success ? match.Groups[1].Value : "";
    }

    private class KinopoiskVideosDto { public List<VideoItem>? items { get; set; } }
    private class VideoItem { public string? name { get; set; } public string? site { get; set; } public string? url { get; set; } }
    private class KinopoiskSimilarDto { public List<SimilarItem>? films { get; set; } }
    private class SimilarItem
    {
        public int filmId { get; set; }
        public string? nameRu { get; set; }
        public string? nameEn { get; set; }
        public int? year { get; set; }
        public double? ratingKinopoisk { get; set; }
        public string? posterUrl { get; set; }
        public string? type { get; set; }
    }
}