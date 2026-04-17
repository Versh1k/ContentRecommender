namespace ContentRecommender.Core.Services;

public interface IMovieApiUrlBuilder
{
    string BuildSearchUrl(int? genreId = null, string? keyword = null);
}