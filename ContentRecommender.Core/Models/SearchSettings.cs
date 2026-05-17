namespace ContentRecommender.Core.Models;

public class SearchSettings
{
    public SearchLimits Limits { get; set; } = new();
    public float MoodScoreThreshold { get; set; }
    public List<string> FallbackGenres { get; set; } = new();
    public SearchUi Ui { get; set; } = new();
}

public class SearchLimits
{
    public int MoviesByGenre { get; set; }
    public int BooksByGenre { get; set; }
    public int MoviesByText { get; set; }
    public int BooksByText { get; set; }
    public int MaxDisplayResults { get; set; }
}

public class SearchUi
{
    public int SearchInputMaxLength { get; set; }
    public int FavoritesRefreshDelayMs { get; set; }
}