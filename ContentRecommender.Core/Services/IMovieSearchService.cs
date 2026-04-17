using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;

public interface IMovieSearchService
{
    Task<List<Movie>> SearchByGenresAsync(List<string> genres, ContentTypeCategory type, int limit = 15, Guid? seed = null);
    Task<List<Movie>> SearchByTextAsync(string query, ContentTypeCategory type, int limit = 15, Guid? seed = null);
    Task<List<Movie>> SearchMoviesAsync(SearchCriteria criteria);
}