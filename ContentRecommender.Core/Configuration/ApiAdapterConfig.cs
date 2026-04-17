using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Configuration;

public class ApiAdapterConfig
{
    public string ActiveProvider { get; set; } = "Kinopoisk";
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
}

public class ProviderConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyHeader { get; set; } = "X-API-KEY";
    public int DefaultRatingFrom { get; set; } = 5;
    public int DefaultLimit { get; set; } = 40;

    public UrlTemplates Urls { get; set; } = new();
    public Dictionary<string, List<string>> MoodToGenres { get; set; } = new();
    public FieldMapping FieldMapping { get; set; } = new();
    public Dictionary<string, string> GenreParameters { get; set; } = new();
    public Dictionary<string, ContentTypeCategory> TypeMapping { get; set; } = new();
}

public class UrlTemplates
{
    public string SearchByGenres { get; set; } = "?genres={genreId}&limit={limit}&ratingFrom={ratingFrom}&order=RATING";
    public string SearchByText { get; set; } = "?keyword={query}&limit={limit}&ratingFrom={ratingFrom}&order=RATING";
}

public class FieldMapping
{
    public string RootArray { get; set; } = "items";
    public string Id { get; set; } = "kinopoiskId";
    public string Title { get; set; } = "nameRu";
    public string TitleFallback { get; set; } = "nameEn";
    public string Description { get; set; } = "description";
    public string Year { get; set; } = "year";
    public string Rating { get; set; } = "ratingKinopoisk";
    public string Genres { get; set; } = "genres";
    public string GenreName { get; set; } = "genre";
    public string Duration { get; set; } = "movieLength";
    public string Directors { get; set; } = "directors";
    public string DirectorName { get; set; } = "name";
    public string PosterUrl { get; set; } = "posterUrl";
    public string Type { get; set; } = "type";
}