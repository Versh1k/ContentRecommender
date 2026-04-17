using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;

namespace ContentRecommender.Web.Services.MovieSearch;

public class MovieCategoryResolver : IMovieCategoryResolver
{
    private readonly ApiAdapterConfig _config;

    public MovieCategoryResolver(IOptions<ApiAdapterConfig> options)
    {
        _config = options.Value;
    }

    private ProviderConfig Current => _config.Providers[_config.ActiveProvider];

    public ContentTypeCategory DetermineCategory(string? type, IEnumerable<string>? genres, int? duration)
    {
        if (!string.IsNullOrEmpty(type) && Current.TypeMapping.TryGetValue(type, out var category))
            return category;

        var genreList = genres?.ToList() ?? new();
        if (genreList.Any(g => g.Contains("аниме", StringComparison.OrdinalIgnoreCase) ||
                               g.Contains("anime", StringComparison.OrdinalIgnoreCase)))
            return ContentTypeCategory.Cartoon;
        if (genreList.Any(g => g.Contains("мульт", StringComparison.OrdinalIgnoreCase) ||
                               g.Contains("cartoon", StringComparison.OrdinalIgnoreCase)))
            return ContentTypeCategory.Cartoon;
        if (duration.HasValue && duration < 45)
            return ContentTypeCategory.ShortFilm;

        return ContentTypeCategory.FeatureFilm;
    }
}