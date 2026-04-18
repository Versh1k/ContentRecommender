using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;

namespace ContentRecommender.Web.Services.MovieSearch;

public class MovieApiUrlBuilder : IMovieApiUrlBuilder
{
    private readonly MovieApiOptions _options;

    public MovieApiUrlBuilder(IOptions<MovieApiOptions> options)
    {
        _options = options.Value;
    }

    private ProviderConfig Current => _options.Providers[_options.ActiveProvider];

    public string BuildSearchUrl(int? genreId = null, string? keyword = null)
    {
        var template = genreId.HasValue
            ? Current.Urls["SearchByGenres"]
            : Current.Urls["SearchByText"];

        return template.Replace("{genre}", genreId?.ToString() ?? "")
                       .Replace("{query}", Uri.EscapeDataString(keyword ?? ""))
                       .Replace("{limit}", Current.DefaultLimit.ToString())
                       .Replace("{ratingFrom}", Current.DefaultRatingFrom.ToString());
    }
}