using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;

namespace ContentRecommender.Web.Services.MovieSearch;

public class MovieApiUrlBuilder : IMovieApiUrlBuilder
{
    private readonly ApiAdapterConfig _config;

    public MovieApiUrlBuilder(IOptions<ApiAdapterConfig> options)
    {
        _config = options.Value;
    }

    private ProviderConfig Current => _config.Providers[_config.ActiveProvider];

    public string BuildSearchUrl(int? genreId = null, string? keyword = null)
    {
        var template = genreId.HasValue
            ? Current.Urls.SearchByGenres
            : Current.Urls.SearchByText;

        var url = template
            .Replace("{genreId}", genreId?.ToString() ?? "")
            .Replace("{query}", Uri.EscapeDataString(keyword ?? ""))
            .Replace("{limit}", Current.DefaultLimit.ToString())
            .Replace("{ratingFrom}", Current.DefaultRatingFrom.ToString());

        return url;
    }
}