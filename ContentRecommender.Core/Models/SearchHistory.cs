namespace ContentRecommender.Core.Models;

public class SearchHistory
{
    public int Id { get; set; }

    // Связь с пользователем
    public string UserId { get; set; } = string.Empty;
    public virtual AppUser? User { get; set; }

    // Параметры поиска
    public string Query { get; set; } = string.Empty;
    public string? SearchCriteriaJson { get; set; } // Сохраняем весь критерий в JSON
    public ContentFormat? SearchedFormat { get; set; }
    public ContentTypeCategory? SearchedCategory { get; set; }
    public BookCategory? SearchedBookCategory { get; set; }
    public int ResultsCount { get; set; }
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}