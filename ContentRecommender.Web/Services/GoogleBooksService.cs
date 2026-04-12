using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using ContentRecommender.Web.ML.Services;
using System.Text.Json;

namespace ContentRecommender.Web.Services;

public class GoogleBooksService : IContentExternalService
{
    private readonly HttpClient _http;
    private readonly GoogleBooksConfig _cfg;
    private readonly IMoodAnalysisService _mood;
    private readonly Random _rnd = new();

    public GoogleBooksService(HttpClient http, GoogleBooksConfig cfg, IMoodAnalysisService mood)
    {
        _http = http;
        _cfg = cfg;
        _mood = mood;
    }

    public async Task<List<Book>> SearchBooksAsync(SearchCriteria criteria)
    {
        var genres = criteria.Genres?.Any() == true ? criteria.Genres : GetRandomGenres();
        return await SearchWithFallback(genres, criteria.Mood, 15, criteria.RandomSeed);
    }

    public async Task<List<Book>> SearchWithFallback(List<string> keywords, MoodType? mood = null, int limit = 15, Guid? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value.GetHashCode()) : _rnd;
        var books = new List<Book>();

        var genreKeywords = keywords.Where(k => !k.StartsWith("keyword:")).ToList();
        var keywordQuery = keywords.FirstOrDefault(k => k.StartsWith("keyword:"))?.Replace("keyword:", "");

        if (genreKeywords.Any())
        {
            var query = string.Join(" OR ", genreKeywords.Select(g => $"subject:{g}"));
            var url = $"{_cfg.BaseUrl}?q={Uri.EscapeDataString(query)}&maxResults=40&orderBy=relevance&printType=books";
            books = await FetchBooks(url, limit, random);
        }

        if (!string.IsNullOrEmpty(keywordQuery) && books.Count < limit)
        {
            var url = $"{_cfg.BaseUrl}?q={Uri.EscapeDataString(keywordQuery)}&maxResults=40&orderBy=relevance&printType=books";
            var keywordBooks = await FetchBooks(url, limit, random);
            books.AddRange(keywordBooks.Where(b => !books.Any(x => x.ExternalId == b.ExternalId)));
        }

        return books.OrderByDescending(b => b.Rating ?? 0).Take(limit).ToList();
    }

    private async Task<List<Book>> FetchBooks(string url, int limit, Random random)
    {
        try
        {
            var startIndex = random.Next(0, 41);
            url += $"&startIndex={startIndex}";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return new();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GoogleBooksResponse>(json);

            if (result?.items == null)
            {
                return new();
            }

            var shuffled = result.items.OrderBy(x => random.Next()).ToList();

            var books = new List<Book>();
            foreach (var item in shuffled)
            {
                var book = MapToBook(item);
                if (IsValidBook(book))
                {
                    books.Add(book);
                    if (books.Count >= limit) break;
                }
            }
            return books;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoogleBooks] Ошибка: {ex.Message}");
            return new();
        }
    }
    private List<string> GetRandomGenres()
    {
        var genres = new[] { "фантастика", "детектив", "роман", "приключения", "мистика", "триллер", "фэнтези", "биография" };
        return new List<string> { genres[_rnd.Next(genres.Length)] };
    }

    private bool IsValidBook(Book book)
    {
        if (book.Pages.HasValue && (book.Pages < 80 || book.Pages > 1500)) return false;
        if (book.Year.HasValue && book.Year < 1950) return false;
        if (string.IsNullOrEmpty(book.CoverUrl)) return false;
        return true;
    }

    private Book MapToBook(BookItem item)
    {
        var info = item.volumeInfo;
        var coverUrl = info?.imageLinks?.thumbnail;
        if (!string.IsNullOrEmpty(coverUrl))
        {
            coverUrl = coverUrl
                .Replace("&zoom=1", "&zoom=3")
                .Replace("http://", "https://");
        }

        var rating = info?.averageRating ?? (info?.pageCount.HasValue == true
            ? Math.Round(3.5 + Math.Min(1.5, info.pageCount.Value / 500.0), 1)
            : 3.5);
        rating = Math.Min(5.0, Math.Max(2.5, rating));

        var description = info?.description ?? info?.title ?? "";
        var mood = _mood.AnalyzeMood(description) ?? MoodType.Everyday;

        return new Book
        {
            Title = info?.title ?? "Без названия",
            Author = info?.authors?.FirstOrDefault() ?? "Неизвестен",
            Description = info?.description ?? "Описание отсутствует",
            Pages = info?.pageCount,
            Year = ExtractYear(info?.publishedDate),
            CoverUrl = coverUrl,
            Rating = rating,
            Genres = info?.categories?.ToList() ?? new List<string>(),
            MoodTags = new List<MoodType> { mood },
            Format = ContentFormat.Book,
            ExternalId = item.id,
            Source = "GoogleBooks",
            BookCategory = BookCategory.Fiction
        };
    }

    private int? ExtractYear(string? publishedDate)
    {
        if (string.IsNullOrEmpty(publishedDate)) return null;
        return int.TryParse(publishedDate.Length >= 4 ? publishedDate[..4] : "", out int year) ? year : null;
    }

    public Task<List<Movie>> SearchMoviesAsync(SearchCriteria criteria) => Task.FromResult(new List<Movie>());
    public Task<List<Movie>> SearchMoviesByKeywordsAsync(List<string> keywords, SearchCriteria.SearchContentType type) => Task.FromResult(new List<Movie>());
    public Task<List<Book>> SearchBooksByKeywordsAsync(List<string> keywords, SearchCriteria.SearchContentType type) => Task.FromResult(new List<Book>());

    private class GoogleBooksResponse { public List<BookItem>? items { get; set; } }
    private class BookItem { public string? id { get; set; } public VolumeInfo? volumeInfo { get; set; } }
    private class VolumeInfo
    {
        public string? title { get; set; }
        public List<string>? authors { get; set; }
        public string? description { get; set; }
        public string? publishedDate { get; set; }
        public int? pageCount { get; set; }
        public double? averageRating { get; set; }
        public ImageLinks? imageLinks { get; set; }
        public List<string>? categories { get; set; }
    }
    private class ImageLinks { public string? thumbnail { get; set; } public string? smallThumbnail { get; set; } }
}