using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Helpers;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;

namespace ContentRecommender.Web.Services.MovieSearch;

public class GenericMovieSearchService : IMovieSearchService
{
    private readonly HttpClient _http;
    private readonly MovieApiOptions _options;
    private readonly IMovieApiUrlBuilder _urlBuilder;
    private readonly IMovieApiResponseParser _parser;
    private readonly IGenreMapper _genreMapper;
    private readonly Random _rnd = new();

    public GenericMovieSearchService(HttpClient http,
                                     IOptions<MovieApiOptions> options,
                                     IMovieApiUrlBuilder urlBuilder,
                                     IMovieApiResponseParser parser,
                                     IGenreMapper genreMapper)
    {
        _http = http;
        _options = options.Value;
        _urlBuilder = urlBuilder;
        _parser = parser;
        _genreMapper = genreMapper;
    }

    private ProviderConfig Current => _options.Providers[_options.ActiveProvider];

    public async Task<List<Movie>> SearchByGenresAsync(List<string> genres, ContentTypeCategory type, int limit = 15, Guid? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value.GetHashCode()) : _rnd;
        var allMovies = new List<Movie>();

        var genreParams = genres
            .Select(g => _genreMapper.GetGenreParameter(g, ContentFormat.Movie))
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .ToList();

        if (!genreParams.Any() && !string.IsNullOrEmpty(Current.Defaults?.FallbackGenreId))
            genreParams.Add(Current.Defaults.FallbackGenreId);

        Console.WriteLine($"[SearchByGenres] GenreParams=[{string.Join(",", genreParams)}], TargetType={type}");

        foreach (var genreParam in genreParams)
        {
            var url = _urlBuilder.BuildSearchUrl(genreId: int.TryParse(genreParam, out var id) ? id : null);
            var fetched = await FetchPage(url, type, limit * (Current.Defaults?.SearchMultiplier ?? 1), random);
            Console.WriteLine($"[SearchByGenres] Fetched {fetched.Count} movies for genre {genreParam}");
            foreach (var movie in fetched)
            {
                if (!allMovies.Any(m => m.ExternalId == movie.ExternalId))
                    allMovies.Add(movie);
            }
        }

        Console.WriteLine($"[SearchByGenres] Before dedup: {allMovies.Count}");
        return allMovies.OrderByDescending(m => m.Rating ?? 0).Take(limit).ToList();
    }

    public async Task<List<Movie>> SearchByTextAsync(string query, ContentTypeCategory type, int limit = 15, Guid? seed = null)
    {
        if (string.IsNullOrWhiteSpace(query)) return new();
        var random = seed.HasValue ? new Random(seed.Value.GetHashCode()) : _rnd;
        var url = _urlBuilder.BuildSearchUrl(keyword: query);
        return await FetchPage(url, type, limit * (Current.Defaults?.SearchMultiplier ?? 1), random)
            .ContinueWith(t => t.Result.Take(limit).ToList());
    }

    private async Task<List<Movie>> FetchPage(string url, ContentTypeCategory targetType, int limit, Random random)
    {
        try
        {
            var separator = url.Contains('?') ? '&' : '?';
            var pageRange = Current.Defaults?.PageRange ?? new[] { 1, 4 };
            url += $"{separator}page={random.Next(pageRange[0], pageRange[1])}";

            Console.WriteLine($"[FetchPage] Request: {url}");

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[FetchPage] API error: {response.StatusCode}");
                return new();
            }

            var json = await response.Content.ReadAsStringAsync();
            var movies = _parser.Parse(json, targetType);

            Console.WriteLine($"[FetchPage] Parsed {movies.Count} movies, TargetType={targetType}");

            if (targetType != ContentTypeCategory.Any)
            {
                var before = movies.Count;
                movies = movies.Where(m => m.Category == targetType).ToList();
                Console.WriteLine($"[FetchPage] Filtered by {targetType}: {before} → {movies.Count}");
            }

            return movies.Take(limit).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FetchPage] Exception: {ex.Message}");
            return new();
        }
    }

    public Task<List<Movie>> SearchMoviesAsync(SearchCriteria criteria)
    {
        var type = ContentTypeMapper.MapToCategory(criteria.SelectedContentType);
        return SearchByGenresAsync(criteria.Genres ?? new(), type, 15, criteria.RandomSeed);
    }
}