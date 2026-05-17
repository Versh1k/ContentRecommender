using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;

public interface IBookSearchService
{
    Task<List<Book>> SearchByGenresAsync(List<string> genres, int limit = 15, Guid? seed = null);
    Task<List<Book>> SearchByTextAsync(string query, int limit = 15, Guid? seed = null);
    Task<List<Book>> SearchByMoodAsync(string? mood, int limit = 15);
}