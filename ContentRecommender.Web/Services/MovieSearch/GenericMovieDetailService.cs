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
            Trailers = await GetTrailersAsync(externalId)
        };
    }

    public async Task<List<VideoDto>> GetTrailersAsync(string externalId)
    {
        var vs = Current.VideoSettings;
        if (vs == null || !Current.Urls.TryGetValue("GetVideos", out var videosTemplate) || string.IsNullOrEmpty(videosTemplate))
            return new();

        var url = $"{Current.BaseUrl}{videosTemplate.Replace("{id}", externalId)}";
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var trailers = new List<VideoDto>();

        if (JsonParserHelper.TryGetProperty(root, vs.RootArrayKey, out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                var site = JsonParserHelper.GetString(item, vs.SiteKey)?.ToLowerInvariant();
                bool isAllowed = site != null && vs.AllowedSites?.Any(s => site.Contains(s, StringComparison.OrdinalIgnoreCase)) == true;

                if (isAllowed)
                {
                    var name = JsonParserHelper.GetString(item, vs.NameKey) ?? Current.Defaults?.DefaultTitle;
                    var videoUrl = JsonParserHelper.GetString(item, vs.UrlKey);
                    var ytId = ExtractVideoId(videoUrl, vs.YouTubeIdRegex);
                    if (!string.IsNullOrEmpty(ytId))
                        trailers.Add(new VideoDto { Title = name, YouTubeId = ytId });
                }
            }
        }
        return trailers.Take(Current.Limits?.MaxTrailers ?? int.MaxValue).ToList();
    }

    public async Task<List<MovieSummaryDto>> GetSimilarMoviesAsync(string externalId, int limit = 9)
    {
        var ss = Current.SimilarSettings;
        if (ss == null || !Current.Urls.TryGetValue("GetSimilar", out var similarTemplate) || string.IsNullOrEmpty(similarTemplate))
            return new();

        var url = $"{Current.BaseUrl}{similarTemplate.Replace("{id}", externalId)}";

        try
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var similar = new List<MovieSummaryDto>();

            if (JsonParserHelper.TryGetProperty(root, ss.RootArrayKey, out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray().Take(ss.MaxResults ?? int.MaxValue))
                {
                    var id = JsonParserHelper.GetString(item, ss.IdKey);
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

    private static string ExtractVideoId(string? url, string regexPattern)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(regexPattern)) return string.Empty;
        var match = Regex.Match(url, regexPattern);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}