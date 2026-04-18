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
        var url = $"{Current.BaseUrl}/{externalId}";
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

    public Task<List<BookSummaryDto>> GetSimilarBooksAsync(string externalId, int limit = 6)
        => Task.FromResult(new List<BookSummaryDto>());
}