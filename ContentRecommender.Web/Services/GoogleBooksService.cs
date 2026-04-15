using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using ContentRecommender.Web.ML.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ContentRecommender.Web.Services;

/// <summary>
/// Сервис для работы с Google Books API (полностью на русском, с новым MoodType)
/// </summary>
public class GoogleBooksService
{
    private readonly HttpClient _http;
    private readonly GoogleBooksConfig _cfg;
    private readonly IMoodAnalysisService _mood;
    private readonly Random _rnd = new();

    // Русские жанры → subject для Google Books API
    private static readonly Dictionary<string, string> RussianGenreToSubject = new()
    {
        {"фантастика", "subject:science fiction"},
        {"фэнтези", "subject:fantasy"},
        {"детектив", "subject:mystery OR subject:detective"},
        {"триллер", "subject:thriller"},
        {"роман", "subject:romance OR subject:love story"},
        {"приключения", "subject:adventure"},
        {"ужасы", "subject:horror"},
        {"классика", "subject:literature OR subject:classic"},
        {"биография", "subject:biography"},
        {"мотивация", "subject:motivational"},
        {"комедия", "subject:humor OR subject:comedy"},
        {"драма", "subject:drama"},
        {"мистика", "subject:mystery"},
        {"поэзия", "subject:poetry"},
        {"история", "subject:history"},
        {"наука", "subject:science"},
        {"военный", "subject:military OR subject:war"},
        {"криминал", "subject:crime"},
        {"повседневность", "subject:domestic fiction"}
    };

    // Категории для случайного поиска
    private static readonly string[] BookSubjects =
    {
        "subject:fiction", "subject:literature", "subject:novel",
        "subject:mystery", "subject:thriller", "subject:fantasy",
        "subject:science fiction", "subject:romance", "subject:adventure"
    };

    public GoogleBooksService(HttpClient http, GoogleBooksConfig cfg, IMoodAnalysisService mood)
    {
        _http = http;
        _cfg = cfg;
        _mood = mood;
    }

    /// <summary>
    /// Поиск книг с fallback-стратегией (по жанрам → настроение → случайные)
    /// Используется в PerformSearch
    /// </summary>
    public async Task<List<Book>> SearchBooksByKeywordsWithFallbackAsync(
        List<string> keywords,
        SearchCriteria.SearchContentType contentType,
        int limit = 10)
    {
        var books = new List<Book>();

        if (keywords != null && keywords.Any())
            books = await SearchByGenresRussian(keywords, limit);

        if (books.Count < limit && keywords != null && keywords.Any())
        {
            var mood = MapFirstKeywordToMood(keywords[0]);
            if (mood.HasValue)
            {
                var moodBooks = await SearchByMood(mood.Value, limit - books.Count);
                MergeUniqueBooks(books, moodBooks);
            }
        }

        if (books.Count < limit)
        {
            var randomBooks = await SearchRandom(limit - books.Count);
            MergeUniqueBooks(books, randomBooks);
        }

        // Фильтрация по типу контента (у вас только AllBooks)
        if (contentType != SearchCriteria.SearchContentType.AllBooks)
            return new List<Book>();

        return books.OrderByDescending(b => b.Rating).Take(limit).ToList();
    }

    // ========== ОСНОВНЫЕ МЕТОДЫ ПОИСКА ==========

    public async Task<List<Book>> SearchByGenresRussian(List<string> keywords, int limit = 15)
    {
        if (keywords == null || !keywords.Any())
            return await SearchRandom(limit);

        var subjects = new List<string>();
        foreach (var kw in keywords.Take(3))
        {
            var lowerKw = kw.ToLowerInvariant();
            bool mapped = false;
            foreach (var pair in RussianGenreToSubject)
            {
                if (lowerKw.Contains(pair.Key))
                {
                    subjects.Add(pair.Value);
                    mapped = true;
                    break;
                }
            }
            if (!mapped)
                subjects.Add($"intitle:{Uri.EscapeDataString(kw)}");
        }

        var query = string.Join(" OR ", subjects);
        var url = $"{_cfg.BaseUrl}?q={Uri.EscapeDataString(query)}&maxResults=40&orderBy=relevance&printType=books";
        return await FetchBooksWithRetry(url, limit);
    }

    public async Task<List<Book>> SearchByMood(MoodType mood, int limit = 15)
    {
        var subject = MapMoodToSubject(mood);
        var url = $"{_cfg.BaseUrl}?q={subject}&maxResults=40&orderBy=relevance&printType=books";
        return await FetchBooksWithRetry(url, limit);
    }

    public async Task<List<Book>> SearchRandom(int limit = 15)
    {
        var randomSubject = BookSubjects[_rnd.Next(BookSubjects.Length)];
        var url = $"{_cfg.BaseUrl}?q={randomSubject}&maxResults=30&orderBy=relevance&printType=books";
        return await FetchBooksWithRetry(url, limit);
    }

    public async Task<List<Book>> SearchBooksAsync(SearchCriteria criteria)
    {
        var genres = criteria.Genres?.Any() == true ? criteria.Genres : GetRandomGenres();
        return await SearchByGenresRussian(genres, 15);
    }

    private async Task<List<Book>> FetchBooksWithRetry(string url, int limit, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<GoogleBooksResponse>(json);
                    return ParseBooksFromResponse(result, limit);
                }
                else if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    Console.WriteLine($"[GoogleBooks] Слишком много запросов (429). Пауза 30 секунд...");
                    await Task.Delay(30000);
                    attempt--;
                    continue;
                }
                else if (attempt == maxRetries)
                {
                    Console.WriteLine($"[GoogleBooks] Ошибка {response.StatusCode} после {maxRetries} попыток.");
                    return new List<Book>();
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[GoogleBooks] Попытка {attempt} не удалась: {ex.Message}");
                if (attempt == maxRetries) return new List<Book>();
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleBooks] Ошибка: {ex.Message}");
                return new List<Book>();
            }
        }
        return new List<Book>();
    }

    private List<Book> ParseBooksFromResponse(GoogleBooksResponse? result, int limit)
    {
        if (result?.items == null) return new List<Book>();
        var books = new List<Book>();
        foreach (var item in result.items)
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

    private void MergeUniqueBooks(List<Book> target, List<Book> source)
    {
        foreach (var book in source)
            if (!target.Any(b => b.ExternalId == book.ExternalId))
                target.Add(book);
    }

    /// <summary>
    /// Маппинг нового MoodType на subject'ы Google Books
    /// </summary>
    private string MapMoodToSubject(MoodType mood)
    {
        return mood switch
        {
            MoodType.Romantic => "subject:romance OR subject:love story",
            MoodType.Mysterious => "subject:mystery OR subject:detective",
            MoodType.Sad => "subject:drama OR subject:tragedy",
            MoodType.Funny => "subject:humor OR subject:comedy",
            MoodType.Crime => "subject:crime OR subject:mystery",
            MoodType.Military => "subject:military OR subject:war",
            MoodType.Everyday => "subject:domestic fiction OR subject:family",
            MoodType.Tense => "subject:thriller",
            MoodType.Horror => "subject:horror",
            MoodType.Inspiring => "subject:biography OR subject:motivational",
            MoodType.Epic => "subject:fantasy OR subject:adventure OR subject:science fiction",
            MoodType.Adventure => "subject:adventure",
            _ => "subject:fiction"
        };
    }

    private MoodType? MapFirstKeywordToMood(string keyword)
    {
        var lower = keyword.ToLowerInvariant();
        if (lower.Contains("романт") || lower.Contains("любов")) return MoodType.Romantic;
        if (lower.Contains("тайн") || lower.Contains("загад") || lower.Contains("детектив")) return MoodType.Mysterious;
        if (lower.Contains("груст") || lower.Contains("печал") || lower.Contains("тоск")) return MoodType.Sad;
        if (lower.Contains("весел") || lower.Contains("смешн") || lower.Contains("юмор")) return MoodType.Funny;
        if (lower.Contains("криминал") || lower.Contains("преступ")) return MoodType.Crime;
        if (lower.Contains("воен") || lower.Contains("войн") || lower.Contains("солдат")) return MoodType.Military;
        if (lower.Contains("повседнев") || lower.Contains("быт") || lower.Contains("жизнь")) return MoodType.Everyday;
        if (lower.Contains("напряж") || lower.Contains("триллер")) return MoodType.Tense;
        if (lower.Contains("страш") || lower.Contains("ужас")) return MoodType.Horror;
        if (lower.Contains("вдохнов") || lower.Contains("мотивац")) return MoodType.Inspiring;
        if (lower.Contains("эпик") || lower.Contains("героическ")) return MoodType.Epic;
        if (lower.Contains("приключ")) return MoodType.Adventure;
        return null;
    }
    public async Task<List<Book>> SearchByTextAsync(string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Book>();

        var encodedQuery = Uri.EscapeDataString(query.Trim());
        var url = $"{_cfg.BaseUrl}?q={encodedQuery}&maxResults=40&orderBy=relevance&printType=books";
        return await FetchBooksWithRetry(url, limit);
    }

    private List<string> GetRandomGenres()
    {
        var genres = new[] { "фантастика", "детектив", "роман", "приключения", "мистика", "триллер" };
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
            coverUrl = coverUrl.Replace("&zoom=1", "&zoom=3").Replace("http://", "https://");

        var rating = info?.averageRating ?? (info?.pageCount.HasValue == true
            ? Math.Round(3.5 + Math.Min(1.5, info.pageCount.Value / 500.0), 1)
            : 3.5);
        rating = Math.Min(5.0, Math.Max(2.5, rating));

        var description = info?.description ?? info?.title ?? "";
        var mood = _mood.AnalyzeMood(description) ?? MoodType.Epic; // <-- исправление: если null, то Epic

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
            MoodTags = new List<MoodType> { mood }, // теперь mood точно не null
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

    // ========== ВЛОЖЕННЫЕ КЛАССЫ ДЛЯ JSON ==========
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