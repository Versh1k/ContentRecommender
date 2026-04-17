using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;

public interface IMovieDetailService
{
    Task<MovieDetailDto?> GetMovieDetailsAsync(string externalId);
    Task<List<VideoDto>> GetTrailersAsync(string externalId);
    Task<List<MovieSummaryDto>> GetSimilarMoviesAsync(string externalId, int limit = 6);
}