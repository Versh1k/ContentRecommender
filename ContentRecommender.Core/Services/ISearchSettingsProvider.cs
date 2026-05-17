namespace ContentRecommender.Core.Services;

public interface ISearchSettingsProvider
{
    int GetMovieLimitByGenre();
    int GetBookLimitByGenre();
    int GetMovieLimitByText();
    int GetBookLimitByText();
    int GetMaxDisplayResults();
    float GetMoodScoreThreshold();
    IEnumerable<string> GetFallbackGenres();
    int GetSearchInputMaxLength();
    int GetFavoritesRefreshDelayMs();
}