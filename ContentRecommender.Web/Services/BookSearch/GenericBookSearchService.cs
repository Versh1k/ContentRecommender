using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;

namespace ContentRecommender.Web.Services.BookSearch;

public class GenericBookSearchService : IBookSearchService
{
    private readonly HttpClient _http;
    private readonly BookApiOptions _options;
    private readonly IBookResponseParser _parser;
    private readonly IGenreMapper _genreMapper;
    private readonly Random _rnd = new();

    public GenericBookSearchService(HttpClient http,
                                    IOptions<BookApiOptions> options,
                                    IBookResponseParser parser,
                                    IGenreMapper genreMapper)
    {
        _http = http;
        _options = options.Value;
        _parser = parser;
        _genreMapper = genreMapper;
    }

    private ProviderConfig Current => _options.Providers[_options.ActiveProvider];

    public async Task<List<Book>> SearchByGenresAsync(List<string> genres, int limit = 15, Guid? seed = null)
    {
        var queries = genres
            .Select(g => _genreMapper.GetGenreParameter(g, ContentFormat.Book))
            .Where(q => !string.IsNullOrEmpty(q))
            .Distinct()
            .ToList();

        if (!queries.Any())
            queries.Add(Current.FallbackSubject);

        var books = new List<Book>();
        foreach (var query in queries.Take(2))
        {
            if (books.Count >= limit) break;
            var url = BuildUrl(query, limit);
            var fetched = await FetchPage(url, limit);
            books.AddRange(fetched.Where(b => !books.Any(x => x.ExternalId == b.ExternalId)));
        }
        return books.OrderByDescending(b => b.Rating).Take(limit).ToList();
    }

    public async Task<List<Book>> SearchByTextAsync(string query, int limit = 15, Guid? seed = null)
    {
        if (string.IsNullOrWhiteSpace(query)) return new();
        var url = BuildUrl(query, limit);
        return await FetchPage(url, limit);
    }

    public async Task<List<Book>> SearchByMoodAsync(string mood, int limit = 15)
    {
        var moodKey = mood.ToString();
        if (Current.MoodToSubjects.TryGetValue(moodKey, out var subject))
            return await SearchByTextAsync(subject, limit);
        return await SearchByTextAsync(Current.FallbackSubject, limit);
    }

    private string BuildUrl(string query, int limit)
    {
        var template = Current.Urls["SearchByText"];
        return template.Replace("{query}", Uri.EscapeDataString(query))
                       .Replace("{limit}", limit.ToString());
    }

    private async Task<List<Book>> FetchPage(string url, int limit)
    {
        int maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await _http.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    await Task.Delay(1000 * (i + 1));
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookSearch] Error {response.StatusCode}: {errorContent}");
                    return new();
                }

                var json = await response.Content.ReadAsStringAsync();
                var books = _parser.Parse(json).Take(limit).ToList();
                Console.WriteLine($"[BookSearch] Parsed {books.Count} books");
                return books;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BookSearch] Exception: {ex.Message}");
                if (i == maxRetries - 1) return new();
                await Task.Delay(1000 * (i + 1));
            }
        }
        return new();
    }
}