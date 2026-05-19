using ContentRecommender.Core.Models;
namespace ContentRecommender.Data.Repositories;
public interface IFavoritesRepository
{
    Task<FavoriteItem?> GetFavoriteAsync(string userId, string externalId, string source);
    Task<List<FavoriteItem>> GetUserFavoritesAsync(string userId, ContentFormat? format = null);
    Task<bool> AddToFavoritesAsync(string userId, FavoriteItem favorite);
    Task<bool> RemoveFromFavoritesAsync(string userId, string externalId, string source);
    Task<bool> IsFavoriteAsync(string userId, string externalId, string source);
    Task<bool> UpdateStatusAsync(string userId, string externalId, string source, FavoriteStatus status);
}