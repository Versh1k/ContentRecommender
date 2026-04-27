using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Helpers;
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
                 ?? "Без названия",
            Description = JsonParserHelper.GetString(root, Fm.Description),
            Year = JsonParserHelper.GetInt32(root, Fm.Year),
            Rating = JsonParserHelper.GetDouble(root, Fm.Rating),
            Genres = ExtractStringArray(root, Fm.Genres, Fm.GenreName),
            DurationMinutes = JsonParserHelper.GetInt32(root, Fm.Duration),
            Director = ExtractFirstString(root, Fm.Directors, Fm.DirectorName),
            Actors = ExtractStringArray(root, Fm.Actors, Fm.ActorName).Take(10).ToList(),
            CoverUrl = NormalizePosterUrl(JsonParserHelper.GetString(root, Fm.PosterUrl)),
            Format = DetermineFormat(root),
            Trailers = await GetTrailersAsync(externalId)
        };
    }

    public async Task<List<VideoDto>> GetTrailersAsync(string externalId)
    {
        if (!Current.Urls.TryGetValue("GetVideos", out var videosTemplate) || string.IsNullOrEmpty(videosTemplate))
            return new();

        var url = $"{Current.BaseUrl}{videosTemplate.Replace("{id}", externalId)}";
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var trailers = new List<VideoDto>();

        if (JsonParserHelper.TryGetProperty(root, "items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                var site = JsonParserHelper.GetString(item, "site")?.ToLower();
                if (site?.Contains("youtube") == true)
                {
                    var name = JsonParserHelper.GetString(item, "name") ?? "Трейлер";
                    var videoUrl = JsonParserHelper.GetString(item, "url");
                    var ytId = ExtractYouTubeId(videoUrl);
                    if (!string.IsNullOrEmpty(ytId))
                        trailers.Add(new VideoDto { Title = name, YouTubeId = ytId });
                }
            }
        }
        return trailers.Take(3).ToList();
    }

    public async Task<List<MovieSummaryDto>> GetSimilarMoviesAsync(string externalId, int limit = 9)
    {
        if (!Current.Urls.TryGetValue("GetSimilar", out var similarTemplate) || string.IsNullOrEmpty(similarTemplate))
            return new();

        var url = $"{Current.BaseUrl}{similarTemplate.Replace("{id}", externalId)}";

        try
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Similar] API returned {response.StatusCode} for ID {externalId}, returning empty");
                return new();
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var similar = new List<MovieSummaryDto>();

            if (JsonParserHelper.TryGetProperty(root, "items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray().Take(limit))
                {
                    var id = JsonParserHelper.GetString(item, "filmId");
                    if (string.IsNullOrEmpty(id)) continue;

                    similar.Add(new MovieSummaryDto
                    {
                        ExternalId = id,
                        Title = JsonParserHelper.GetString(item, "nameRu")
                             ?? JsonParserHelper.GetString(item, "nameEn")
                             ?? "Без названия",
                        CoverUrl = NormalizePosterUrl(JsonParserHelper.GetString(item, "posterUrl")),
                        Year = null,
                        Rating = null
                    });
                }
            }
            Console.WriteLine($"[Similar] Found {similar.Count} similar movies for {externalId}");
            return similar;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Similar] Error for {externalId}: {ex.Message}");
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

    private static string? NormalizePosterUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        return url.Replace("/300x450/", "/700x1000/")
                  .Replace("http://", "https://");
    }

    private ContentFormat DetermineFormat(JsonElement root)
    {
        var type = JsonParserHelper.GetString(root, Fm.Type)?.ToUpper() ?? "";
        if (Current.TypeMapping.TryGetValue(type, out var mapped))
        {
            return mapped switch
            {
                "TvSeries" => ContentFormat.Series,
                "Cartoon" => ContentFormat.Cartoon,
                _ => ContentFormat.Movie
            };
        }
        return ContentFormat.Movie;
    }

    private static string ExtractYouTubeId(string? url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        var match = Regex.Match(url, @"(?:v=|\/)([a-zA-Z0-9_-]{11})");
        return match.Success ? match.Groups[1].Value : "";
    }
}