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
        var root = doc.RootElement;

        if (JsonParserHelper.TryGetProperty(root, Fm.RootArray, out var rootArray)
            && rootArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in rootArray.EnumerateArray())
            {
                var movie = MapToMovie(item);
                if (!string.IsNullOrEmpty(movie.ExternalId) && !string.IsNullOrEmpty(movie.Title))
                    movies.Add(movie);
            }
        }
        else if (!string.IsNullOrEmpty(Fm.RootArray))
        {
            var movie = MapToMovie(root);
            if (!string.IsNullOrEmpty(movie.ExternalId) && !string.IsNullOrEmpty(movie.Title))
                movies.Add(movie);
        }

        return movies;
    }

    private Movie MapToMovie(JsonElement item)
    {
        string externalId = JsonParserHelper.GetString(item, Fm.Id) ?? Guid.NewGuid().ToString();

        string title = JsonParserHelper.GetString(item, Fm.Title)
                    ?? (!string.IsNullOrEmpty(Fm.TitleFallback)
                        ? JsonParserHelper.GetString(item, Fm.TitleFallback)
                        : null)
                    ?? "Без названия";

        int? year = JsonParserHelper.GetInt32(item, Fm.Year);
        double? rating = JsonParserHelper.GetDouble(item, Fm.Rating);
        string? description = JsonParserHelper.GetString(item, Fm.Description);
        int? duration = JsonParserHelper.GetInt32(item, Fm.Duration);
        string? poster = JsonParserHelper.GetString(item, Fm.PosterUrl);
        string? type = JsonParserHelper.GetString(item, Fm.Type);

        Console.WriteLine($"[Parser] ExternalId='{externalId}', Title='{title}'");

        var genres = new List<string>();
        if (!string.IsNullOrEmpty(Fm.Genres) && !string.IsNullOrEmpty(Fm.GenreName) &&
            JsonParserHelper.TryGetProperty(item, Fm.Genres, out var genresElem) &&
            genresElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var g in genresElem.EnumerateArray())
            {
                if (JsonParserHelper.TryGetProperty(g, Fm.GenreName, out var nameElem))
                {
                    var genreName = nameElem.GetString();
                    if (!string.IsNullOrEmpty(genreName))
                        genres.Add(genreName);
                }
            }
        }

        string? director = null;
        if (!string.IsNullOrEmpty(Fm.Directors) && !string.IsNullOrEmpty(Fm.DirectorName) &&
            JsonParserHelper.TryGetProperty(item, Fm.Directors, out var dirsElem) &&
            dirsElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var d in dirsElem.EnumerateArray())
            {
                if (JsonParserHelper.TryGetProperty(d, Fm.DirectorName, out var nameElem))
                {
                    director = nameElem.GetString();
                    if (!string.IsNullOrEmpty(director)) break;
                }
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
            Source = _options.ActiveProvider,
            IsCompleted = true,
            Category = category
        };
    }
}