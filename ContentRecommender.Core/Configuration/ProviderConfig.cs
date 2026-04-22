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
    public string ExternalButtonText { get; set; } = "Открыть";
    public string ExternalUrlTemplate { get; set; } = "{BaseUrl}/{id}";
    public string FallbackSubject { get; set; } = "subject:fiction";
}