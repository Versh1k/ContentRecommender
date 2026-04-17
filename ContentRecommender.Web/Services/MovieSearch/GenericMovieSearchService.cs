using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;
using ContentRecommender.Core.Helpers;

namespace ContentRecommender.Web.Services.MovieSearch;

public class GenericMovieSearchService : IMovieSearchService
{
    private readonly HttpClient _http;
    private readonly ApiAdapterConfig _config;
    private readonly IMovieApiUrlBuilder _urlBuilder;
    private readonly IMovieApiResponseParser _parser;
    private readonly IGenreMapper _genreMapper;
    private readonly Random _rnd = new();

    public GenericMovieSearchService(
        HttpClient http,
        IOptions<ApiAdapterConfig> options,
        IMovieApiUrlBuilder urlBuilder,
        IMovieApiResponseParser parser,
        IGenreMapper genreMapper)
    {
        _http = http;
        _config = options.Value;
        _urlBuilder = urlBuilder;
        _parser = parser;
        _genreMapper = genreMapper;

        var current = _config.Providers[_config.ActiveProvider];
        _http.BaseAddress = new Uri(current.BaseUrl);
        if (!_http.DefaultRequestHeaders.Contains(current.ApiKeyHeader))
            _http.DefaultRequestHeaders.Add(current.ApiKeyHeader, current.ApiKey);
    }

    private ProviderConfig Current => _config.Providers[_config.ActiveProvider];

    public async Task<List<Movie>> SearchByGenresAsync(List<string> genres, ContentTypeCategory type, int limit = 15, Guid? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value.GetHashCode()) : _rnd;
        var movies = new List<Movie>();

        var genreIds = genres
            .Select(g => _genreMapper.GetGenreId(g))
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .Distinct()
            .ToList();

        if (!genreIds.Any())
            genreIds.Add(_genreMapper.GetRandomGenreIdForType(type, random));

        foreach (var genreId in genreIds)
        {
            if (movies.Count >= limit) break;

            var url = _urlBuilder.BuildSearchUrl(genreId: genreId);
            var fetched = await FetchPage(url, type, limit - movies.Count, random);
            Console.WriteLine($"[MovieSearch] Жанр {genreId}: получили {fetched.Count} фильмов");

            //movies.AddRange(fetched.Where(m => !movies.Any(x => x.ExternalId == m.ExternalId)));
            movies.AddRange(fetched);

            Console.WriteLine($"[MovieSearch] Всего после жанра {genreId}: {movies.Count} уникальных фильмов");
        }

        return movies.OrderByDescending(m => m.Rating ?? 0).Take(limit).ToList();
    }

    public async Task<List<Movie>> SearchByTextAsync(string query, ContentTypeCategory type, int limit = 15, Guid? seed = null)
    {
        if (string.IsNullOrWhiteSpace(query)) return new();
        var random = seed.HasValue ? new Random(seed.Value.GetHashCode()) : _rnd;
        var url = _urlBuilder.BuildSearchUrl(keyword: query);
        return await FetchPage(url, type, limit, random);
    }

    private async Task<List<Movie>> FetchPage(string url, ContentTypeCategory targetType, int limit, Random random)
    {
        try
        {
            var separator = url.Contains('?') ? '&' : '?';
            url += $"{separator}page={random.Next(1, 4)}";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            return _parser.Parse(json, targetType).Take(limit).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MovieSearch] Ошибка: {ex.Message}");
            return new();
        }
    }
    public async Task<List<Movie>> SearchMoviesAsync(SearchCriteria criteria)
    {
        var type = ContentTypeMapper.MapToCategory(criteria.SelectedContentType);
        var genres = criteria.Genres?.Any() == true
            ? criteria.Genres
            : new List<string>();
        return await SearchByGenresAsync(genres, type, 15, criteria.RandomSeed);
    }
}