namespace ContentRecommender.Core.Configuration;

public class KinopoiskConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl {  get; set; } = string.Empty;
    public int DefaultRatingFrom { get; set; } = 5;
    public int DefaultLimit { get; set; } = 40;
    public Dictionary<string, int> GenreMapping { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<int>> TypeGenres { get; set; } = new();
}