using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Helpers;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ContentRecommender.Web.Services.BookSearch;

public class ConfigurableBookParser : IBookResponseParser
{
    private readonly BookApiOptions _options;

    public ConfigurableBookParser(IOptions<BookApiOptions> options)
    {
        _options = options.Value;
    }

    private ProviderConfig Current => _options.Providers[_options.ActiveProvider];
    private FieldMapping Fm => Current.FieldMapping;

    public List<Book> Parse(string json)
    {
        var books = new List<Book>();
        using var doc = JsonDocument.Parse(json);

        if (!JsonParserHelper.TryGetProperty(doc.RootElement, Fm.RootArray, out var rootArray) ||
            rootArray.ValueKind != JsonValueKind.Array)
            return books;

        foreach (var item in rootArray.EnumerateArray())
        {
            var book = MapToBook(item);
            if (IsValidBook(book))
                books.Add(book);
        }
        return books;
    }

    private Book MapToBook(JsonElement item)
    {
        string id = JsonParserHelper.GetString(item, Fm.Id) ?? string.Empty;
        string title = JsonParserHelper.GetString(item, Fm.Title) ?? "Без названия";
        string? description = JsonParserHelper.GetString(item, Fm.Description);
        int? year = ExtractYear(JsonParserHelper.GetString(item, Fm.Year));
        double? rating = JsonParserHelper.GetDouble(item, Fm.Rating);
        string? cover = JsonParserHelper.GetString(item, Fm.PosterUrl)
            ?.Replace("&zoom=1", "&zoom=3")
            .Replace("http://", "https://");
        string? author = GetFirstFromArray(item, Fm.Authors);
        int? pages = JsonParserHelper.GetInt32(item, Fm.Pages);
        var genres = GetStringArray(item, Fm.Genres);

        return new Book
        {
            Title = title,
            Description = description,
            Year = year,
            Rating = rating ?? 0,
            CoverUrl = cover,
            Author = author,
            Pages = pages,
            Genres = genres,
            Format = ContentFormat.Book,
            ExternalId = id,
            Source = _options.ActiveProvider,
            BookCategory = BookCategory.Fiction
        };
    }

    private bool IsValidBook(Book book)
    {
        if (book.Pages.HasValue && (book.Pages < 20 || book.Pages > 2000)) return false;
        if (book.Year.HasValue && book.Year < 1000) return false;
        if (string.IsNullOrEmpty(book.CoverUrl)) return false;
        return true;
    }

    private static List<string> GetStringArray(JsonElement element, string path)
    {
        var result = new List<string>();
        if (JsonParserHelper.TryGetProperty(element, path, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String)
                    result.Add(item.GetString()!);
        return result;
    }

    private static string? GetFirstFromArray(JsonElement element, string path)
    {
        if (JsonParserHelper.TryGetProperty(element, path, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String)
                    return item.GetString();
        return null;
    }

    private static int? ExtractYear(string? publishedDate)
    {
        if (string.IsNullOrEmpty(publishedDate)) return null;
        return publishedDate.Length >= 4 && int.TryParse(publishedDate[..4], out var y) ? y : null;
    }
}