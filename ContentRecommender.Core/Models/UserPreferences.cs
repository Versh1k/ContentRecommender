namespace ContentRecommender.Core.Models;

public class UserPreferences
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public virtual AppUser? User { get; set; }

    public List<string> PreferredMoods { get; set; } = new();
    public List<ContentTypeCategory> PreferredContentTypes { get; set; } = new();
    public List<BookCategory> PreferredBookCategories { get; set; } = new();
    public List<string> FavoriteGenres { get; set; } = new();

    public bool DarkMode { get; set; } = true;
    public string? Language { get; set; } = "ru";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}