using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;

namespace ContentRecommender.Web.Services.MovieSearch;

public class MovieCategoryResolver : IMovieCategoryResolver
{
    private readonly MovieApiOptions _options;
    public MovieCategoryResolver(IOptions<MovieApiOptions> options) => _options = options.Value;
    private ProviderConfig Current => _options.Providers[_options.ActiveProvider];

    public ContentTypeCategory DetermineCategory(string? type, IEnumerable<string>? genres, int? duration)
    {
        if (genres?.Any() == true && Current.CategoryDetection?.GenreKeywords != null)
        {
            var genresLower = genres.Select(g => g?.ToLowerInvariant()).Where(g => !string.IsNullOrEmpty(g)).ToHashSet();

            if (Current.CategoryDetection.GenreKeywords.TryGetValue("Cartoon", out var cartoonKeywords))
            {
                var keywords = cartoonKeywords.Select(k => k.ToLowerInvariant()).ToHashSet();
                if (genresLower.Any(g => keywords.Contains(g)))
                    return ContentTypeCategory.Cartoon;
            }

            if (Current.CategoryDetection.GenreKeywords.TryGetValue("TvSeries", out var seriesKeywords))
            {
                var keywords = seriesKeywords.Select(k => k.ToLowerInvariant()).ToHashSet();
                if (genresLower.Any(g => keywords.Contains(g)))
                    return ContentTypeCategory.TvSeries;
            }
        }

        if (!string.IsNullOrEmpty(type) && Current.TypeMapping != null)
        {
            var cmp = Current.CategoryDetection?.TypeMappingCaseSensitive == true
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
            var match = Current.TypeMapping.FirstOrDefault(kvp => kvp.Key.Equals(type, cmp));
            if (!string.IsNullOrEmpty(match.Value) && Enum.TryParse<ContentTypeCategory>(match.Value, out var cat))
                return cat;
        }

        if (!string.IsNullOrEmpty(Current.CategoryDetection?.FallbackCategory) &&
            Enum.TryParse<ContentTypeCategory>(Current.CategoryDetection.FallbackCategory, out var fallback))
        {
            return fallback;
        }

        return ContentTypeCategory.FeatureFilm;
    }
}