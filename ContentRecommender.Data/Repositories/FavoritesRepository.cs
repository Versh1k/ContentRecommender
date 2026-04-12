using ContentRecommender.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ContentRecommender.Data.Repositories;

public class FavoritesRepository : IFavoritesRepository
{
    private readonly AppDbContext _context;

    public FavoritesRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FavoriteItem?> GetFavoriteAsync(string userId, string externalId, string source)
    {
        return await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ExternalId == externalId && f.Source == source);
    }

    public async Task<List<FavoriteItem>> GetUserFavoritesAsync(string userId, ContentFormat? format = null)
    {
        var query = _context.Favorites.Where(f => f.UserId == userId);
        if (format.HasValue)
        {
            query = query.Where(f => f.Format == format.Value);
        }
        query = query.OrderByDescending(f => f.AddedAt);

        return await query.ToListAsync();
    }

    public async Task<bool> AddToFavoritesAsync(string userId, FavoriteItem favorite)
    {
        try
        {
            favorite.UserId = userId;
            favorite.AddedAt = DateTime.UtcNow;

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка добавления в избранное: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RemoveFromFavoritesAsync(string userId, string externalId, string source)
    {
        try
        {
            var favorite = await GetFavoriteAsync(userId, externalId, source);
            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsFavoriteAsync(string userId, string externalId, string source)
    {
        return await _context.Favorites.AnyAsync(f => f.UserId == userId &&f.ExternalId == externalId &&f.Source == source);
    }
}