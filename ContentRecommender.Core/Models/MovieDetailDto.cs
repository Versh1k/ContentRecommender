namespace ContentRecommender.Core.Models;

public class MovieDetailDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverUrl { get; set; }
    public int? Year { get; set; }
    public double? Rating { get; set; }
    public List<string> Genres { get; set; } = new();
    public int? DurationMinutes { get; set; }
    public string? Director { get; set; }
    public List<string> Actors { get; set; } = new();
    public List<VideoDto> Trailers { get; set; } = new();
    public ContentFormat Format { get; set; }
    public bool IsFavorite { get; set; }
}

public class VideoDto
{
    public string Title { get; set; } = string.Empty;
    public string YouTubeId { get; set; } = string.Empty;
}

public class MovieSummaryDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public int? Year { get; set; }
    public double? Rating { get; set; }
    public ContentFormat Format { get; set; }
}