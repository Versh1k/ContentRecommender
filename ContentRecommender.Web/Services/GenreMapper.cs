using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;

public class GenreMapper : IGenreMapper
{
    private readonly MovieApiOptions _movieOptions;
    private readonly BookApiOptions _bookOptions;

    public GenreMapper(IOptions<MovieApiOptions> movieOptions, IOptions<BookApiOptions> bookOptions)
    {
        _movieOptions = movieOptions.Value;
        _bookOptions = bookOptions.Value;
    }

    public string? GetGenreParameter(string genreName, ContentFormat format)
    {
        if (string.IsNullOrWhiteSpace(genreName)) return null;
        var normalized = genreName.ToLowerInvariant().Trim();
        ProviderConfig provider = format == ContentFormat.Book
            ? _bookOptions.Providers[_bookOptions.ActiveProvider]
            : _movieOptions.Providers[_movieOptions.ActiveProvider];
        return provider.GenreParameters.GetValueOrDefault(normalized);
    }
}