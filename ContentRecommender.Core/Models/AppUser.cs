using Microsoft.AspNetCore.Identity;

namespace ContentRecommender.Core.Models;

public class AppUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual UserPreferences? Preferences { get; set; }
    public virtual ICollection<FavoriteItem> Favorites { get; set; } = new List <FavoriteItem>();
    public virtual ICollection<SearchHistory> SearchHistory { get; set; } = new List<SearchHistory>();
}