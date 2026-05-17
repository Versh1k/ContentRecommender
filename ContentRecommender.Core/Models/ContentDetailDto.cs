namespace ContentRecommender.Core.Models;

public class ContentDetailDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public ContentFormat Format { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverUrl { get; set; }

    public int? Year { get; set; }
    public double? Rating { get; set; }
    public List<string>? Genres { get; set; }


    public int? DurationMinutes { get; set; }
    public string? Director { get; set; }
    public List<string>? Actors { get; set; }
    public List<TrailerDto>? Trailers { get; set; }

    public string? Author { get; set; }
    public int? Pages { get; set; }

    public bool IsFavorite { get; set; }
    public List<ContentDetailDto>? SimilarItems { get; set; }
}

public class TrailerDto
{
    public string Title { get; set; } = string.Empty;
    public string YouTubeId { get; set; } = string.Empty;
    public string ThumbnailUrl => $"https://img.youtube.com/vi/{YouTubeId}/mqdefault.jpg";
    public string WatchUrl => $"https://www.youtube.com/watch?v={YouTubeId}";
}