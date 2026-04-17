using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;

public interface IMovieApiResponseParser
{
    List<Movie> Parse(string json, ContentTypeCategory targetType);
}