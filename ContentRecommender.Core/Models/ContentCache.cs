using System.ComponentModel.DataAnnotations;

namespace ContentRecommender.Core.Models;

public class ContentCache
{
    [MaxLength(100)]
    public string ExternalId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Source { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    
    [MaxLength(500)]
    public string? CoverUrl { get; set; }

    [MaxLength(500)]
    public string? SearchKey { get; set; }

    public int? Year { get; set; }
    public double? Rating { get; set; }
    public string? GenresJson { get; set; }
    public string? MoodTagsJson { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Director { get; set; }
    public string? Author { get; set; }
    public int? Pages { get; set; }
    public bool? IsCompleted { get; set; }

    public ContentFormat Format { get; set; }
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public ContentTypeCategory Category { get; set; } = ContentTypeCategory.FeatureFilm;
}