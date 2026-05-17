using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Helpers;

public static class ContentTypeMapper
{
    public static ContentTypeCategory MapToCategory(SearchCriteria.SearchContentType type)
    {
        return type switch
        {
            SearchCriteria.SearchContentType.All => ContentTypeCategory.Any,
            SearchCriteria.SearchContentType.Movies => ContentTypeCategory.FeatureFilm,
            SearchCriteria.SearchContentType.TvSeries => ContentTypeCategory.TvSeries,
            SearchCriteria.SearchContentType.Cartoons => ContentTypeCategory.Cartoon,
            _ => ContentTypeCategory.FeatureFilm
        };
    }
}