using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;

public interface IMovieCategoryResolver
{
    ContentTypeCategory DetermineCategory(string? type, IEnumerable<string>? genres, int? duration);
}