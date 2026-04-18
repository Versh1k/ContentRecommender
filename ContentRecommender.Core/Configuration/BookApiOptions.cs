namespace ContentRecommender.Core.Configuration;

public class BookApiOptions
{
    public string ActiveProvider { get; set; } = "DefaultBook";
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
}