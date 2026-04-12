using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;
public interface IContentOrchestrator
{
    Task<List<Movie>> SearchMoviesAsync(SearchCriteria criteria);
    Task<List<Book>> SearchBooksAsync(SearchCriteria criteria);
}