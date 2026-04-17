using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ContentRecommender.Web.Services.MovieSearch;

public class ConfigurableJsonParser : IMovieApiResponseParser
{
    private readonly ApiAdapterConfig _config;
    private readonly IMovieCategoryResolver _categoryResolver;

    public ConfigurableJsonParser(IOptions<ApiAdapterConfig> options, IMovieCategoryResolver categoryResolver)
    {
        _config = options.Value;
        _categoryResolver = categoryResolver;
    }

    private ProviderConfig Current => _config.Providers[_config.ActiveProvider];

    public List<Movie> Parse(string json, ContentTypeCategory targetType)
    {
        var movies = new List<Movie>();
        using var doc = JsonDocument.Parse(json);

        if (!TryGetProperty(doc.RootElement, Current.FieldMapping.RootArray, out var rootArray) ||
            rootArray.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine($"[Parser] RootArray '{Current.FieldMapping.RootArray}' не найден или не массив");
            return movies;
        }

        int total = rootArray.GetArrayLength();
        int categoryMismatch = 0, ratingLow = 0, noCover = 0;
        Console.WriteLine($"[Parser] Всего элементов: {total}");

        foreach (var item in rootArray.EnumerateArray())
        {
            var movie = MapToMovie(item);
            /*
            if (movie.Category != targetType)
            {
                categoryMismatch++;
                continue;
            }
            */
            if ((movie.Rating ?? 0) < Current.DefaultRatingFrom)
            {
                ratingLow++;
                continue;
            }
            if (string.IsNullOrEmpty(movie.CoverUrl))
            {
                noCover++;
                continue;
            }
            movies.Add(movie);
        }

        Console.WriteLine($"[Parser] Категория не совпала: {categoryMismatch}, рейтинг <{Current.DefaultRatingFrom}: {ratingLow}, нет обложки: {noCover}");
        Console.WriteLine($"[Parser] Прошло фильтрацию: {movies.Count}");
        return movies;
    }

    private Movie MapToMovie(JsonElement item)
    {
        var fm = Current.FieldMapping;

        string externalId = GetString(item, fm.Id) ?? "";
        string title = GetString(item, fm.Title) ?? GetString(item, fm.TitleFallback) ?? "Без названия";
        int? year = GetInt32(item, fm.Year);
        double? rating = GetDouble(item, fm.Rating);
        string? description = GetString(item, fm.Description);
        int? duration = GetInt32(item, fm.Duration);
        string? poster = GetString(item, fm.PosterUrl);
        string? type = GetString(item, fm.Type);

        // Жанры
        var genres = new List<string>();
        if (TryGetProperty(item, fm.Genres, out var genresElem) && genresElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var g in genresElem.EnumerateArray())
                if (TryGetProperty(g, fm.GenreName, out var nameElem))
                    genres.Add(nameElem.GetString() ?? "");
        }

        // Режиссёр
        string? director = null;
        if (TryGetProperty(item, fm.Directors, out var dirsElem) && dirsElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var d in dirsElem.EnumerateArray())
                if (TryGetProperty(d, fm.DirectorName, out var nameElem))
                {
                    director = nameElem.GetString();
                    break;
                }
        }

        var category = _categoryResolver.DetermineCategory(type, genres, duration);

        return new Movie
        {
            Title = title,
            Description = description,
            Year = year,
            Rating = rating,
            Genres = genres,
            DurationMinutes = duration,
            Director = director,
            CoverUrl = poster,
            Format = ContentFormat.Movie,
            ExternalId = externalId,
            Source = _config.ActiveProvider,
            IsCompleted = true,
            Category = category
        };
    }

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
    {
        if (TryGetProperty(element, path, out var val) && val.ValueKind == JsonValueKind.String)
            return val.GetString();
        return null;
    }

    private static int? GetInt32(JsonElement element, string path)
    {
        if (TryGetProperty(element, path, out var val) && val.ValueKind == JsonValueKind.Number)
            return val.GetInt32();
        return null;
    }

    private static double? GetDouble(JsonElement element, string path)
    {
        if (TryGetProperty(element, path, out var val) && val.ValueKind == JsonValueKind.Number)
            return val.GetDouble();
        return null;
    }
}