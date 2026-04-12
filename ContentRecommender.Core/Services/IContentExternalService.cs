using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;

public interface IContentExternalService
{
    Task<List<Movie>> SearchMoviesAsync(SearchCriteria criteria);
    Task<List<Book>> SearchBooksAsync(SearchCriteria criteria);
    Task<List<Movie>> SearchMoviesByKeywordsAsync(List<string> keywords, SearchCriteria.SearchContentType contentType);
    Task<List<Book>> SearchBooksByKeywordsAsync(List<string> keywords, SearchCriteria.SearchContentType contentType);
}