using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;

namespace ContentRecommender.Web.Services.BookSearch;

public class GenericBookDetailService : IBookDetailService
{
    private readonly HttpClient _http;
    private readonly BookApiOptions _options;
    private readonly IBookResponseParser _parser;

    public GenericBookDetailService(HttpClient http,
                                    IOptions<BookApiOptions> options,
                                    IBookResponseParser parser)
    {
        _http = http;
        _options = options.Value;
        _parser = parser;
    }

    private ProviderConfig Current => _options.Providers[_options.ActiveProvider];

    public async Task<BookDetailDto?> GetBookDetailsAsync(string externalId)
    {
        if (!Current.Urls.TryGetValue("GetDetails", out var detailsTemplate))
            return null;

        var url = $"{Current.BaseUrl}{detailsTemplate.Replace("{id}", externalId)}";
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var book = _parser.Parse(json).FirstOrDefault();
        if (book == null) return null;

        return new BookDetailDto
        {
            ExternalId = book.ExternalId,
            Source = _options.ActiveProvider,
            Title = book.Title,
            Description = book.Description,
            CoverUrl = book.CoverUrl,
            Year = book.Year,
            Rating = book.Rating,
            Genres = book.Genres,
            Author = book.Author,
            Pages = book.Pages
        };
    }

    public async Task<List<BookSummaryDto>> GetSimilarBooksAsync(string externalId, int limit = 6)
    {
        var book = await GetBookDetailsAsync(externalId);
        if (book == null || !book.Genres.Any()) return new();

        var genre = book.Genres.First();
        var genreParam = Current.GenreParameters.GetValueOrDefault(genre.ToLower(), genre);
        var query = $"subject:{Uri.EscapeDataString(genreParam)}";

        if (!Current.Urls.TryGetValue("SearchByText", out var searchTemplate))
            return new();

        var url = $"{Current.BaseUrl}{searchTemplate.Replace("{query}", query).Replace("{limit}", limit.ToString())}";

        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new();

        var json = await response.Content.ReadAsStringAsync();
        var books = _parser.Parse(json);

        return books
            .Where(b => b.ExternalId != externalId)
            .Take(limit)
            .Select(b => new BookSummaryDto
            {
                ExternalId = b.ExternalId,
                Title = b.Title,
                CoverUrl = b.CoverUrl,
                Year = b.Year,
                Rating = b.Rating
            })
            .ToList();
    }
}