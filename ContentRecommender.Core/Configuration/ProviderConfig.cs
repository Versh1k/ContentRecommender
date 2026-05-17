namespace ContentRecommender.Core.Configuration;

public class ProviderConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyHeader { get; set; } = string.Empty;
    public double DefaultRatingFrom { get; set; } = 5.0;
    public int DefaultLimit { get; set; } = 40;
    public Dictionary<string, string> Urls { get; set; } = new();
    public FieldMapping FieldMapping { get; set; } = new();
    public Dictionary<string, string> GenreParameters { get; set; } = new();
    public Dictionary<string, string> TypeMapping { get; set; } = new();
    public Dictionary<string, List<string>> MoodToGenres { get; set; } = new();
    public Dictionary<string, string> MoodToSubjects { get; set; } = new();
    public string PublicLinkTemplate { get; set; } = string.Empty;
    public string ExternalButtonText { get; set; } = "Открыть";
    public string ExternalUrlTemplate { get; set; } = "{BaseUrl}/{id}";
    public string FallbackSubject { get; set; } = "subject:fiction";

    public DefaultsConfig Defaults { get; set; } = new();
    public CategoryDetectionConfig CategoryDetection { get; set; } = new();
    public LimitsConfig Limits { get; set; } = new();
    public VideoSettingsConfig VideoSettings { get; set; } = new();
    public SimilarSettingsConfig SimilarSettings { get; set; } = new();
    public PosterNormalizationConfig PosterUrlNormalization { get; set; } = new();
    public Dictionary<string, string> FormatMapping { get; set; } = new();
}

public class DefaultsConfig
{
    public string FallbackGenreId { get; set; } = string.Empty;
    public string DefaultTitle { get; set; } = string.Empty;
    public int[] PageRange { get; set; } = new[] { 1, 4 };
    public int SearchMultiplier { get; set; } = 2;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public string FallbackCategory { get; set; } = string.Empty;
}

public class CategoryDetectionConfig
{
    public string FallbackCategory { get; set; } = string.Empty;
    public Dictionary<string, List<string>> GenreKeywords { get; set; } = new();
    public bool TypeMappingCaseSensitive { get; set; } = false;
}

public class LimitsConfig
{
    public int MaxActors { get; set; } = 10;
    public int MaxTrailers { get; set; } = 3;
}

public class VideoSettingsConfig
{
    public string RootArrayKey { get; set; } = string.Empty;
    public string SiteKey { get; set; } = string.Empty;
    public string NameKey { get; set; } = string.Empty;
    public string UrlKey { get; set; } = string.Empty;
    public List<string> AllowedSites { get; set; } = new();
    public string YouTubeIdRegex { get; set; } = string.Empty;
}

public class SimilarSettingsConfig
{
    public string RootArrayKey { get; set; } = string.Empty;
    public string IdKey { get; set; } = string.Empty;
    public List<string> TitleKeys { get; set; } = new();
    public string PosterKey { get; set; } = string.Empty;
    public int? MaxResults { get; set; }
}

public class PosterNormalizationConfig
{
    public Dictionary<string, string> Replacements { get; set; } = new();
}