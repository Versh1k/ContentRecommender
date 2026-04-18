using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;

namespace ContentRecommender.Web.Services.MovieSearch;

public class MovieCategoryResolver : IMovieCategoryResolver
{
    private readonly MovieApiOptions _options;

    public MovieCategoryResolver(IOptions<MovieApiOptions> options)
    {
        _options = options.Value;
    }

    private ProviderConfig Current => _options.Providers[_options.ActiveProvider];

    public ContentTypeCategory DetermineCategory(string? type, IEnumerable<string>? genres, int? duration)
    {
        if (!string.IsNullOrEmpty(type) && Current.TypeMapping.TryGetValue(type, out var categoryStr))
        {
            if (Enum.TryParse<ContentTypeCategory>(categoryStr, out var category))
                return category;
        }

        return ContentTypeCategory.FeatureFilm;
    }
}