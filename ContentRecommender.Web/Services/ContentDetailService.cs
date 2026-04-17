using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using ContentRecommender.Data.Repositories;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ContentRecommender.Web.Services;

public class ContentDetailService : IContentDetailService
{
    private readonly IMovieDetailService _movieDetail;
    private readonly GoogleBooksConfig _googleBooksConfig;
    private readonly IFavoritesRepository _favoritesRepository;
    private readonly HttpClient _http;

    public ContentDetailService(
        IMovieDetailService movieDetail,
        GoogleBooksConfig googleBooksConfig,
        IFavoritesRepository favoritesRepository,
        HttpClient http)
    {
        _movieDetail = movieDetail;
        _googleBooksConfig = googleBooksConfig;
        _favoritesRepository = favoritesRepository;
        _http = http;

        // Заголовок для Kinopoisk API (используется и в GenericMovieDetailService, но для старого кода книг не нужен)
        // Оставим на случай прямых вызовов, но лучше перенести в IMovieDetailService
    }

    public async Task<ContentDetailDto?> GetContentDetailsAsync(string source, string externalId, string? userId = null)
    {
        if (source.Equals("kinopoisk", StringComparison.OrdinalIgnoreCase))
        {
            var movie = await _movieDetail.GetMovieDetailsAsync(externalId);
            if (movie == null) return null;
            return new ContentDetailDto
            {
                ExternalId = movie.ExternalId,
                Source = movie.Source,
                Format = movie.Format,
                Title = movie.Title,
                Description = movie.Description,
                CoverUrl = movie.CoverUrl,
                Year = movie.Year,
                Rating = movie.Rating,
                Genres = movie.Genres,
                DurationMinutes = movie.DurationMinutes,
                Director = movie.Director,
                Actors = movie.Actors,
                IsFavorite = !string.IsNullOrEmpty(userId) &&
                    await _favoritesRepository.IsFavoriteAsync(userId, externalId, "Kinopoisk")
            };
        }
        else if (source.Equals("googlebooks", StringComparison.OrdinalIgnoreCase))
        {
            return await GetBookDetailsAsync(externalId, userId);
        }
        return null;
    }

    public async Task<List<ContentDetailDto>> GetSimilarContentAsync(string source, string externalId, int limit = 6)
    {
        if (source.Equals("kinopoisk", StringComparison.OrdinalIgnoreCase))
        {
            var similar = await _movieDetail.GetSimilarMoviesAsync(externalId, limit);
            return similar.Select(s => new ContentDetailDto
            {
                ExternalId = s.ExternalId,
                Source = "Kinopoisk",
                Title = s.Title,
                CoverUrl = s.CoverUrl,
                Year = s.Year,
                Rating = s.Rating,
                Format = s.Format
            }).ToList();
        }
        return new List<ContentDetailDto>();
    }

    // ========== Старая реализация для книг (временно) ==========
    private async Task<ContentDetailDto?> GetBookDetailsAsync(string externalId, string? userId = null)
    {
        try
        {
            var url = $"{_googleBooksConfig.BaseUrl}/{externalId}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var bookData = JsonSerializer.Deserialize<GoogleBookDto>(json);
            if (bookData?.volumeInfo == null) return null;

            bool isFavorite = !string.IsNullOrEmpty(userId) &&
                await _favoritesRepository.IsFavoriteAsync(userId, externalId, "GoogleBooks");

            return new ContentDetailDto
            {
                ExternalId = externalId,
                Source = "GoogleBooks",
                Format = ContentFormat.Book,
                Title = bookData.volumeInfo.title ?? "Без названия",
                Description = bookData.volumeInfo.description,
                CoverUrl = bookData.volumeInfo.imageLinks?.thumbnail ?? bookData.volumeInfo.imageLinks?.smallThumbnail,
                Year = int.TryParse(bookData.volumeInfo.publishedDate?.Substring(0, 4), out var year) ? year : null,
                Rating = bookData.volumeInfo.averageRating ?? 0,
                Genres = bookData.volumeInfo.categories?.Where(c => !string.IsNullOrEmpty(c)).ToList(),
                Author = bookData.volumeInfo.authors?.FirstOrDefault(),
                Pages = bookData.volumeInfo.pageCount,
                IsFavorite = isFavorite
            };
        }
        catch
        {
            return null;
        }
    }

    // Вспомогательные классы для десериализации Google Books
    private class GoogleBookDto
    {
        public VolumeInfo? volumeInfo { get; set; }
    }

    private class VolumeInfo
    {
        public string? title { get; set; }
        public string? description { get; set; }
        public string? publishedDate { get; set; }
        public double? averageRating { get; set; }
        public List<string>? categories { get; set; }
        public List<string>? authors { get; set; }
        public int? pageCount { get; set; }
        public ImageLinks? imageLinks { get; set; }
    }

    private class ImageLinks
    {
        public string? thumbnail { get; set; }
        public string? smallThumbnail { get; set; }
    }
}