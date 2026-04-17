using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Logging;

namespace ContentRecommender.Web.Services;

public class ContentSearchService : IContentOrchestrator
{
    private readonly IMovieSearchService _movieSearch;
    private readonly GoogleBooksService _googleBooks;
    private readonly IContentCacheService _cache;
    private readonly ILogger<ContentSearchService> _logger;

    public ContentSearchService(
        IMovieSearchService movieSearch,
        GoogleBooksService googleBooks,
        IContentCacheService cache,
        ILogger<ContentSearchService> logger)
    {
        _movieSearch = movieSearch;
        _googleBooks = googleBooks;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<Movie>> SearchMoviesAsync(SearchCriteria criteria)
    {
        var cached = await _cache.GetMoviesFromCacheAsync(criteria);
        if (cached != null && cached.Any())
        {
            _logger.LogInformation("Фильмы из кэша: {Count}", cached.Count);
            return cached;
        }

        var movies = await _movieSearch.SearchMoviesAsync(criteria);
        if (movies.Any())
        {
            await _cache.SaveMoviesToCacheAsync(movies, criteria);
            _logger.LogInformation("Сохранено {Count} фильмов в кэш", movies.Count);
        }
        return movies;
    }

    public async Task<List<Book>> SearchBooksAsync(SearchCriteria criteria)
    {
        var cached = await _cache.GetBooksFromCacheAsync(criteria);
        if (cached != null && cached.Any())
        {
            _logger.LogInformation("Книги из кэша: {Count}", cached.Count);
            return cached;
        }

        var books = await _googleBooks.SearchBooksAsync(criteria);
        if (books.Any())
        {
            await _cache.SaveBooksToCacheAsync(books, criteria);
            _logger.LogInformation("Сохранено {Count} книг в кэш", books.Count);
        }
        return books;
    }
}