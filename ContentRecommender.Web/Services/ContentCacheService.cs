using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ContentRecommender.Web.Services;

public class ContentCacheService : IContentCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ContentCacheService> _logger;

    public ContentCacheService(IMemoryCache cache, ILogger<ContentCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<List<Movie>?> GetMoviesFromCacheAsync(SearchCriteria criteria)
    {
        var key = GetCacheKey(criteria, "Movies");
        _cache.TryGetValue(key, out List<Movie>? movies);
        return Task.FromResult(movies);
    }

    public Task SaveMoviesToCacheAsync(List<Movie> movies, SearchCriteria criteria)
    {
        var key = GetCacheKey(criteria, "Movies");
        _cache.Set(key, movies, TimeSpan.FromHours(24));
        _logger.LogInformation("Сохранено {Count} фильмов в кэш", movies.Count);
        return Task.CompletedTask;
    }

    public Task<List<Book>?> GetBooksFromCacheAsync(SearchCriteria criteria)
    {
        var key = GetCacheKey(criteria, "Books");
        _cache.TryGetValue(key, out List<Book>? books);
        return Task.FromResult(books);
    }

    public Task SaveBooksToCacheAsync(List<Book> books, SearchCriteria criteria)
    {
        var key = GetCacheKey(criteria, "Books");
        _cache.Set(key, books, TimeSpan.FromHours(24));
        _logger.LogInformation("Сохранено {Count} книг в кэш", books.Count);
        return Task.CompletedTask;
    }

    private string GetCacheKey(SearchCriteria criteria, string type)
    {
        var json = JsonSerializer.Serialize(new
        {
            criteria.UserInput,
            criteria.Mood,
            criteria.Genres,
            criteria.SelectedContentType,
            criteria.RandomSeed
        });
        return $"{type}_{json}";
    }
}