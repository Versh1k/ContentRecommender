using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;

public interface IContentCacheService
{
    Task<List<Movie>?> GetMoviesFromCacheAsync(SearchCriteria criteria);
    Task SaveMoviesToCacheAsync(List<Movie> movies, SearchCriteria criteria);
    Task<List<Book>?> GetBooksFromCacheAsync(SearchCriteria criteria);
    Task SaveBooksToCacheAsync(List<Book> books, SearchCriteria criteria);
}