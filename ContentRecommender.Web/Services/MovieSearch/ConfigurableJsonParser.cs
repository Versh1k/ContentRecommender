using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Helpers;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ContentRecommender.Web.Services.MovieSearch;

public class ConfigurableJsonParser : IMovieApiResponseParser
{
    private readonly MovieApiOptions _options;
    private readonly IMovieCategoryResolver _categoryResolver;

    public ConfigurableJsonParser(IOptions<MovieApiOptions> options, IMovieCategoryResolver categoryResolver)
    {
        _options = options.Value;
        _categoryResolver = categoryResolver;
    }

    private ProviderConfig Current => _options.Providers[_options.ActiveProvider];
    private FieldMapping Fm => Current.FieldMapping;

    public List<Movie> Parse(string json, ContentTypeCategory targetType)
    {
        var movies = new List<Movie>();
        using var doc = JsonDocument.Parse(json);

        if (!JsonParserHelper.TryGetProperty(doc.RootElement, Fm.RootArray, out var rootArray) ||
            rootArray.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine($"[ConfigurableJsonParser] RootArray '{Fm.RootArray}' not found or not an array");
            return movies;
        }

        Console.WriteLine($"[ConfigurableJsonParser] Total items from API: {rootArray.GetArrayLength()}");


        foreach (var item in rootArray.EnumerateArray())
        {
            var movie = MapToMovie(item);

            // Логируем причины отсева
            //if (movie.Category != targetType)
            //{
            //    Console.WriteLine($"[ConfigurableJsonParser] Skipped '{movie.Title}' — category {movie.Category} != {targetType}");
            //    continue;
            //}
            if ((movie.Rating ?? 0) < Current.DefaultRatingFrom)
            {
                Console.WriteLine($"[ConfigurableJsonParser] Skipped '{movie.Title}' — rating {movie.Rating} < {Current.DefaultRatingFrom}");
                continue;
            }
            if (string.IsNullOrEmpty(movie.CoverUrl))
            {
                Console.WriteLine($"[ConfigurableJsonParser] Skipped '{movie.Title}' — no cover");
                continue;
            }

            movies.Add(movie);
        }

        Console.WriteLine($"[ConfigurableJsonParser] Passed filters: {movies.Count}");
        return movies;
    }

    private Movie MapToMovie(JsonElement item)
    {
        string externalId = JsonParserHelper.GetString(item, Fm.Id)
                    ?? JsonParserHelper.GetString(item, "kinopoiskId")  // явный запасной вариант
                    ?? Guid.NewGuid().ToString();

        string title = JsonParserHelper.GetString(item, Fm.Title)
                    ?? JsonParserHelper.GetString(item, Fm.TitleFallback)
                    ?? JsonParserHelper.GetString(item, "nameOriginal")
                    ?? "Без названия";

        int? year = JsonParserHelper.GetInt32(item, Fm.Year);
        double? rating = JsonParserHelper.GetDouble(item, Fm.Rating);
        string? description = JsonParserHelper.GetString(item, Fm.Description);
        int? duration = JsonParserHelper.GetInt32(item, Fm.Duration);
        string? poster = JsonParserHelper.GetString(item, Fm.PosterUrl);
        string? type = JsonParserHelper.GetString(item, Fm.Type);

        Console.WriteLine($"[Parser] Extracted ExternalId: '{externalId}' for movie '{title}'");
        var genres = new List<string>();
        if (JsonParserHelper.TryGetProperty(item, Fm.Genres, out var genresElem) && genresElem.ValueKind == JsonValueKind.Array)
            foreach (var g in genresElem.EnumerateArray())
                if (JsonParserHelper.TryGetProperty(g, Fm.GenreName, out var nameElem))
                    genres.Add(nameElem.GetString() ?? "");

        string? director = null;
        if (JsonParserHelper.TryGetProperty(item, Fm.Directors, out var dirsElem) && dirsElem.ValueKind == JsonValueKind.Array)
            foreach (var d in dirsElem.EnumerateArray())
                if (JsonParserHelper.TryGetProperty(d, Fm.DirectorName, out var nameElem))
                {
                    director = nameElem.GetString();
                    break;
                }

        var category = _categoryResolver.DetermineCategory(type, genres, duration);

        Console.WriteLine($"[Parser] Extracted ExternalId: '{externalId}' for movie '{title}'");

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
            Source = _options.ActiveProvider,
            IsCompleted = true,
            Category = category
        };
    }
}