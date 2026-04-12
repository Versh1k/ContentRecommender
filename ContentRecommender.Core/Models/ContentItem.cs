using System.ComponentModel.DataAnnotations;

namespace ContentRecommender.Core.Models;

public abstract class ContentItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public ContentFormat Format { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? CoverUrl { get; set; }

    [MaxLength(100)]
    public string? ExternalId { get; set; }

    [MaxLength(50)]
    public string? Source { get; set; }

    public int? Year { get; set; }

    public double? Rating { get; set; }

    public List<string>? Genres { get; set; }
    public List<MoodType>? MoodTags { get; set; }

    public bool? IsCompleted { get; set; }

    public DateTime? CachedAt { get; set; }
}