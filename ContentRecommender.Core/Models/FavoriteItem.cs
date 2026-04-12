using System.Text.Json;

namespace ContentRecommender.Core.Models;

public class FavoriteItem
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public virtual AppUser? User { get; set; }

    public string ExternalId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public ContentFormat Format { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }

    public string? GenresJson { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<string>? Genres => 
        string.IsNullOrEmpty(GenresJson) ? null : JsonSerializer.Deserialize<List<string>>(GenresJson);
}