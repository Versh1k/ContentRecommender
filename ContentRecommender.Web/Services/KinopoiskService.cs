using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using ContentRecommender.Web.ML.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContentRecommender.Web.Services;

public class KinopoiskService
{
    private readonly HttpClient _http;
    private readonly KinopoiskConfig _cfg;
    private readonly IMoodAnalysisService _mood;
    private readonly Random _rnd = new();

    private static readonly Dictionary<string, int> GenreIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["боевик"] = 1,
        ["фантастика"] = 2,
        ["комедия"] = 3,
        ["драма"] = 4,
        ["детектив"] = 5,
        ["документальный"] = 6,
        ["исторический"] = 7,
        ["мелодрама"] = 8,
        ["приключения"] = 9,
        ["триллер"] = 10,
        ["ужасы"] = 11,
        ["криминал"] = 12,
        ["мультфильм"] = 13,
        ["фэнтези"] = 14,
        ["вестерн"] = 15,
        ["военный"] = 16,
        ["биография"] = 17,
        ["спортивный"] = 19,
        ["музыкальный"] = 20,
        ["семейный"] = 22,
        ["аниме"] = 24,
        ["детский"] = 25,
        ["короткометражка"] = 27,
        ["романтика"] = 8,
        ["мистика"] = 10
    };

    private static readonly Dictionary<ContentTypeCategory, List<int>> TypeGenres = new()
    {
        [ContentTypeCategory.FeatureFilm] = new() { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 14, 15, 16, 17, 19, 20, 22, 27 },
        [ContentTypeCategory.TvSeries] = new() { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 14, 15, 16, 17, 19, 20, 22, 24 },
        [ContentTypeCategory.Cartoon] = new() { 13, 24, 25 },
        [ContentTypeCategory.ShortFilm] = new() { 27 }
    };

    public KinopoiskService(HttpClient http, KinopoiskConfig cfg, IMoodAnalysisService mood)
    {
        _http = http;
        _cfg = cfg;
        _mood = mood;
    }

    public async Task<List<Movie>> SearchMoviesAsync(SearchCriteria criteria)
    {
        var type = MapToType(criteria.SelectedContentType);
        var genres = criteria.Genres?.Any() == true ? criteria.Genres : GetRandomGenresForType(type);
        return await SearchWithFallback(genres, type, 15, criteria.RandomSeed);
    }

    public Task<List<Book>> SearchBooksAsync(SearchCriteria criteria) => Task.FromResult(new List<Book>());

    public async Task<List<Movie>> SearchWithFallback(List<string> keywords, ContentTypeCategory type, int limit = 15, Guid? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value.GetHashCode()) : _rnd;
        var movies = new List<Movie>();

        var keywordQuery = keywords.FirstOrDefault(k => k.StartsWith("keyword:"))?.Replace("keyword:", "").Trim();

        if (!string.IsNullOrEmpty(keywordQuery))
        {
            var url = $"{_cfg.BaseUrl}?keyword={Uri.EscapeDataString(keywordQuery)}&limit=40&ratingFrom=5&order=RATING";
            var fetched = await FetchMovies(url, type, limit, random);
            movies.AddRange(fetched);
            Console.WriteLine($"[Kinopoisk] keyword-поиск '{keywordQuery}': найдено {movies.Count} фильмов");
        }
        else
        {
            var fallbackUrl = $"{_cfg.BaseUrl}?limit=40&ratingFrom=7&order=RATING";
            var fetched = await FetchMovies(fallbackUrl, type, limit, random);
            movies.AddRange(fetched);
            Console.WriteLine($"[Kinopoisk] fallback (без keyword): найдено {movies.Count} фильмов");
        }

        return movies.OrderByDescending(m => m.Rating ?? 0).Take(limit).ToList();
    }

    private async Task<List<Movie>> FetchMovies(string url, ContentTypeCategory targetType, int limit, Random random)
    {
        try
        {
            url += $"&page={random.Next(1, 4)}";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<KinopoiskResponse>(json);
            if (data?.items == null) return new();

            var movies = new List<Movie>();
            foreach (var item in data.items)
            {
                var movie = MapToMovie(item);

                if (!MatchesType(movie, targetType)) continue;
                if ((movie.Rating ?? 0) < 5) continue;
                if (string.IsNullOrEmpty(movie.CoverUrl)) continue;

                movies.Add(movie);
                if (movies.Count >= limit) break;
            }
            foreach (var item in data.items.Take(3))
            {
                Console.WriteLine($"Фильм: {item.nameRu}, жанры: {string.Join(",", item.genres?.Select(g => g.genre) ?? new List<string>())}");
            }

            return movies;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Kinopoisk] Ошибка: {ex.Message}");
            return new();
        }
    }

    private List<string> GetRandomGenresForType(ContentTypeCategory type)
    {
        var genreIds = TypeGenres.GetValueOrDefault(type, TypeGenres[ContentTypeCategory.FeatureFilm]);
        var randomId = genreIds[_rnd.Next(genreIds.Count)];
        return GenreIds.Where(kv => kv.Value == randomId).Select(kv => kv.Key).ToList();
    }

    private bool MatchesType(Movie movie, ContentTypeCategory target) => movie.Category == target;

    private ContentTypeCategory DetermineCategory(FilmItem item)
    {
        var genres = item.genres?.Select(g => g.genre?.ToLower()) ?? Enumerable.Empty<string>();
        var type = item.type?.ToUpper() ?? "";

        if (genres.Contains("аниме") || genres.Contains("anime")) return ContentTypeCategory.Cartoon;
        if (genres.Contains("мультфильм") || genres.Contains("animation") || genres.Contains("cartoon")) return ContentTypeCategory.Cartoon;
        if (genres.Contains("детский") && type != "FILM") return ContentTypeCategory.Cartoon;
        if (item.movieLength.HasValue && item.movieLength < 45) return ContentTypeCategory.ShortFilm;
        if (type.Contains("TV_SERIES") || type.Contains("MINI_SERIES") || type.Contains("TV_SHOW")) return ContentTypeCategory.TvSeries;
        return ContentTypeCategory.FeatureFilm;
    }

    private Movie MapToMovie(FilmItem item)
    {
        var cover = item.posterUrl;
        if (!string.IsNullOrEmpty(cover))
        {
            cover = cover.Replace("https://st.kp.yandex.net/images/film_iphone/", "https://st.kp.yandex.net/images/film_big/")
                         .Replace("/300x450/", "/700x1000/")
                         .Replace("/600x900/", "/700x1000/");
        }

        return new Movie
        {
            Title = item.nameRu ?? item.nameEn ?? item.nameOriginal ?? "Без названия",
            Description = item.description,
            Year = item.year,
            Rating = item.ratingKinopsiok,
            Genres = item.genres?.Select(g => g.genre).Where(g => !string.IsNullOrEmpty(g)).Cast<string>().ToList() ?? new List<string>(),
            DurationMinutes = item.movieLength,
            Director = item.directors?.FirstOrDefault()?.name,
            CoverUrl = cover,
            Format = ContentFormat.Movie,
            ExternalId = item.kinopoiskId.ToString(),
            Source = "Kinopoisk",
            IsCompleted = true,
            Category = DetermineCategory(item)
        };
    }

    private ContentTypeCategory MapToType(SearchCriteria.SearchContentType selected)
    {
        return selected switch
        {
            SearchCriteria.SearchContentType.Movies => ContentTypeCategory.FeatureFilm,
            SearchCriteria.SearchContentType.TvSeries => ContentTypeCategory.TvSeries,
            SearchCriteria.SearchContentType.Cartoons => ContentTypeCategory.Cartoon,
            _ => ContentTypeCategory.FeatureFilm
        };
    }

    public async Task<Movie?> GetBestMovieByGenres(List<string> genreNames, ContentTypeCategory type, int minRating = 5)
    {
        var genreIds = genreNames
            .Select(g => GenreIds.GetValueOrDefault(g.ToLowerInvariant().Trim(), 0))
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (!genreIds.Any())
        {
            Console.WriteLine($"[Kinopoisk] Не найдены ID для жанров: {string.Join(", ", genreNames)}");
            return null;
        }

        var random = new Random();
        var allMovies = new List<Movie>();

        allMovies = allMovies
        .Where(m => m.DurationMinutes >= 70)       // только полный метр
        .Where(m => !string.IsNullOrEmpty(m.CoverUrl)) // с постером
        .ToList();

        foreach (var genreId in genreIds)
        {
            var url = $"{_cfg.BaseUrl}?limit=40&genres={genreId}&ratingFrom={minRating}&order=RATING";
            var fetched = await FetchMovies(url, type, 40, random);
            allMovies.AddRange(fetched);
            Console.WriteLine($"[Kinopoisk] По жанру id={genreId} найдено {fetched.Count} фильмов");
        }

        var bestMovie = allMovies
            .OrderByDescending(m => m.Rating ?? 0)
            .FirstOrDefault();

        if (bestMovie != null)
            Console.WriteLine($"[Kinopoisk] Лучший фильм: {bestMovie.Title} (рейтинг {bestMovie.Rating})");

        return bestMovie;
    }

    public Task<List<Movie>> SearchMoviesByKeywordsAsync(List<string> keywords, SearchCriteria.SearchContentType type) => Task.FromResult(new List<Movie>());
    public Task<List<Book>> SearchBooksByKeywordsAsync(List<string> keywords, SearchCriteria.SearchContentType type) => Task.FromResult(new List<Book>());

    private class KinopoiskResponse { public List<FilmItem> items { get; set; } = new(); }
    private class FilmItem
    {
        public int kinopoiskId { get; set; }
        public string? nameRu { get; set; }
        public string? nameEn { get; set; }
        public string? nameOriginal { get; set; }
        public string? description { get; set; }
        public int? year { get; set; }
        [JsonPropertyName("ratingKinopoisk")] public double? ratingKinopsiok { get; set; }
        public List<GenreItem>? genres { get; set; }
        public int? movieLength { get; set; }
        public List<PersonItem>? directors { get; set; }
        public string? posterUrl { get; set; }
        [JsonPropertyName("type")] public string? type { get; set; }
    }
    private class GenreItem { public string? genre { get; set; } }
    private class PersonItem { public string? name { get; set; } }
}