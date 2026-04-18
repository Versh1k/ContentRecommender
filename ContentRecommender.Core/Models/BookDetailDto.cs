namespace ContentRecommender.Core.Models;

public class BookDetailDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverUrl { get; set; }
    public int? Year { get; set; }
    public double? Rating { get; set; }
    public List<string> Genres { get; set; } = new();
    public string? Author { get; set; }
    public int? Pages { get; set; }
    public ContentFormat Format { get; set; } = ContentFormat.Book;
    public bool IsFavorite { get; set; }
}

public class BookSummaryDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public int? Year { get; set; }
    public double? Rating { get; set; }
}