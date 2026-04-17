using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;

public interface IGenreMapper
{
    int? GetGenreId(string genreName);
    List<int> GetGenreIdsForType(ContentTypeCategory type);
    int GetRandomGenreIdForType(ContentTypeCategory type, Random random);
}