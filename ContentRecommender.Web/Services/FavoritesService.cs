using ContentRecommender.Core.Models;
using ContentRecommender.Data.Repositories;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ContentRecommender.Web.Services;

public interface IFavoritesService
{
    Task<bool> ToggleFavoriteAsync(ContentItem item);
    Task<HashSet<string>> GetUserFavoriteKeysAsync();
    bool IsFavorite(string externalId, string source, HashSet<string> favoriteKeys);
    Task<bool> IsFavoriteAsync(string externalId, string source); // новый метод
    event Action? OnFavoritesChanged;
}

public class FavoritesService : IFavoritesService
{
    private readonly IFavoritesRepository _repository;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly UserManager<AppUser> _userManager;
    private HashSet<string> _favoriteKeys = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public event Action? OnFavoritesChanged;
    public async Task<bool> IsFavoriteAsync(string externalId, string source)
    {
        if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(source))
            return false;

        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
            return false;

        return await _repository.IsFavoriteAsync(userId, externalId, source);
    }
    public FavoritesService(
        IFavoritesRepository repository,
        AuthenticationStateProvider authStateProvider,
        UserManager<AppUser> userManager)
    {
        _repository = repository;
        _authStateProvider = authStateProvider;
        _userManager = userManager;
    }

    public async Task<bool> ToggleFavoriteAsync(ContentItem item)
    {
        await _lock.WaitAsync();
        try
        {
            Console.WriteLine($" ToggleFavoriteAsync: {item.Title} ({item.ExternalId})");

            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine(" Не удалось получить UserId");
                return false;
            }

            if (string.IsNullOrEmpty(item.ExternalId) || string.IsNullOrEmpty(item.Source))
            {
                Console.WriteLine(" Пустой ExternalId или Source");
                return false;
            }

            var key = $"{item.Source}:{item.ExternalId}";
            var actualFormat = item.Format;

            if (item is Movie movie)
            {
                actualFormat = movie.Category switch
                {
                    ContentTypeCategory.TvSeries => ContentFormat.Series,
                    ContentTypeCategory.Cartoon => ContentFormat.Cartoon,
                    _ => ContentFormat.Movie
                };
            }
            bool isFavoriteNow = _favoriteKeys.Contains(key);

            if (isFavoriteNow)
            {
                Console.WriteLine($" Удаление из избранного: {key}");
                if (await _repository.RemoveFromFavoritesAsync(userId, item.ExternalId, item.Source))
                {
                    _favoriteKeys.Remove(key);
                    Console.WriteLine($" Удалено, всего избранного: {_favoriteKeys.Count}");
                    OnFavoritesChanged?.Invoke();
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"➕ Добавление в избранное: {key}");
                var favorite = new FavoriteItem
                {
                    UserId = userId,
                    ExternalId = item.ExternalId,
                    Source = item.Source,
                    Format = actualFormat,
                    Title = item.Title ?? "Без названия",
                    CoverUrl = item.CoverUrl,
                    AddedAt = DateTime.UtcNow
                };

                if (await _repository.AddToFavoritesAsync(userId, favorite))
                {
                    _favoriteKeys.Add(key);
                    Console.WriteLine($" Добавлено, всего избранного: {_favoriteKeys.Count}");
                    OnFavoritesChanged?.Invoke();
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка избранного: {ex.Message}");

            if (ex.Message.Contains("duplicate") || ex.Message.Contains("уникальности") || ex.Message.Contains("23505"))
            {
                var userId = await GetCurrentUserIdAsync();
                if (!string.IsNullOrEmpty(userId))
                {
                    await RefreshFavoritesCache(userId);
                    string key = $"{item.Source}:{item.ExternalId}";
                    bool nowInFav = _favoriteKeys.Contains(key);
                    Console.WriteLine($" Синхронизация: элемент {key} теперь {(nowInFav ? "в избранном" : "не в избранном")}");
                    return nowInFav;
                }
            }

            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<HashSet<string>> GetUserFavoriteKeysAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (!string.IsNullOrEmpty(userId))
            {
                var favorites = await _repository.GetUserFavoritesAsync(userId);
                _favoriteKeys = new HashSet<string>(favorites.Select(f => $"{f.Source}:{f.ExternalId}"));
                Console.WriteLine($" Загружено избранное: {_favoriteKeys.Count} элементов");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка загрузки избранного: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }

        return _favoriteKeys;
    }

    public bool IsFavorite(string externalId, string source, HashSet<string> favoriteKeys)
    {
        if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(source))
        {
            return false;
        }
        return favoriteKeys.Contains($"{source}:{externalId}");
    }

    private async Task<string?> GetCurrentUserIdAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(user.Identity.Name))
        {
            var appUser = await _userManager.FindByEmailAsync(user.Identity.Name);
            userId = appUser?.Id;
        }

        if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(user.Identity.Name))
        {
            var appUser = await _userManager.FindByNameAsync(user.Identity.Name);
            userId = appUser?.Id;
        }

        return userId;
    }
    private async Task RefreshFavoritesCache(string userId)
    {
        var favorites = await _repository.GetUserFavoritesAsync(userId);
        _favoriteKeys = new HashSet<string>(favorites.Select(f => $"{f.Source}:{f.ExternalId}"));
    }
}