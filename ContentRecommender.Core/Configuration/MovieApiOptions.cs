namespace ContentRecommender.Core.Configuration;

public class MovieApiOptions
{
    public string ActiveProvider { get; set; } = "DefaultMovie";
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
}