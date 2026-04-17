using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;

public interface IJsonResponseParser
{
    List<Movie> Parse(string json, ProviderConfig config, ContentTypeCategory targetType);
}