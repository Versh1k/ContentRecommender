namespace ContentRecommender.Core.Models;

public class SearchHistory
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public virtual AppUser? User { get; set; }

    public string Query { get; set; } = string.Empty;
    public string? SearchCriteriaJson { get; set; }
    public ContentFormat? SearchedFormat { get; set; }
    public ContentTypeCategory? SearchedCategory { get; set; }
    public BookCategory? SearchedBookCategory { get; set; }
    public int ResultsCount { get; set; }
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}