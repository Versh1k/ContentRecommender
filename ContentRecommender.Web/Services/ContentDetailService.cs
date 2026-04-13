using ContentRecommender.Core.Models;
using ContentRecommender.Core.Configuration;
using ContentRecommender.Data.Repositories;
using System.Text.Json;

namespace ContentRecommender.Web.Services;

public class ContentDetailService : IContentDetailService
{
    private readonly HttpClient _http;
    private readonly KinopoiskConfig _kinopoiskConfig;
    private readonly GoogleBooksConfig _googleBooksConfig;
    private readonly IFavoritesRepository _favoritesRepository;

    public ContentDetailService(
        HttpClient http,
        KinopoiskConfig kinopoiskConfig,
        GoogleBooksConfig googleBooksConfig,
        IFavoritesRepository favoritesRepository)
    {
        _http = http;
        _kinopoiskConfig = kinopoiskConfig;
        _googleBooksConfig = googleBooksConfig;
        _favoritesRepository = favoritesRepository;

        if (!_http.DefaultRequestHeaders.Contains("X-API-KEY"))
        {
            _http.DefaultRequestHeaders.Add("X-API-KEY", _kinopoiskConfig.ApiKey);
        }
    }

    public async Task<ContentDetailDto?> GetContentDetailsAsync(string source, string externalId, string? userId = null)
    {
        try
        {
            return source.ToLower() switch
            {
                "kinopoisk" => await GetMovieDetailsAsync(externalId, userId),
                "googlebooks" => await GetBookDetailsAsync(externalId, userId),
                _ => null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка получения деталей: {ex.Message}");
            return null;
        }
    }

    private async Task<ContentDetailDto?> GetMovieDetailsAsync(string externalId, string? userId = null)
    {
        try
        {
            var url = $"{_kinopoiskConfig.BaseUrl}/{externalId}";
            Console.WriteLine($" Запрос деталей фильма: {url}");

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($" Ошибка API: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"DEBUG JSON: {json}");
            var filmData = JsonSerializer.Deserialize<KinopoiskFilmDto>(json);
            if (filmData == null) return null;

            var trailers = await GetTrailersAsync(externalId);

            bool isFavorite = false;
            if (!string.IsNullOrEmpty(userId))
                isFavorite = await _favoritesRepository.IsFavoriteAsync(userId, externalId, "Kinopoisk");

            var format = DetermineFormat(filmData);

            // Выбираем постер: сначала posterUrlPreview, если нет – posterUrl
            var poster = filmData.posterUrlPreview ?? filmData.posterUrl;
            if (!string.IsNullOrEmpty(poster))
                poster = poster.Replace("/300x450/", "/700x1000/");

            return new ContentDetailDto
            {
                ExternalId = externalId,
                Source = "Kinopoisk",
                Format = format,
                Title = filmData.nameRu ?? filmData.nameOriginal ?? filmData.nameEn ?? "Без названия",
                Description = filmData.description,
                CoverUrl = poster,
                Year = filmData.year,
                Rating = filmData.ratingKinopoisk,
                Genres = filmData.genres?.Select(g => g.genre).Where(g => !string.IsNullOrEmpty(g)).ToList() ?? new List<string>(),
                DurationMinutes = filmData.filmLength,
                Director = filmData.directors?.FirstOrDefault()?.name,
                Actors = filmData.actors?.Take(10).Select(a => a.name).Where(n => !string.IsNullOrEmpty(n)).ToList(),
                Trailers = trailers,
                IsFavorite = isFavorite
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка: {ex.Message}");
            return null;
        }
    }

    private async Task<ContentDetailDto?> GetBookDetailsAsync(string externalId, string? userId = null)
    {
        try
        {
            var url = $"https://www.googleapis.com/books/v1/volumes/{externalId}";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var bookData = JsonSerializer.Deserialize<GoogleBookDto>(json);

            if (bookData?.volumeInfo == null)
            {
                return null;
            }

            bool isFavorite = false;

            if (!string.IsNullOrEmpty(userId))
            {
                isFavorite = await _favoritesRepository.IsFavoriteAsync(userId, externalId, "GoogleBooks");
            }

            return new ContentDetailDto
            {
                ExternalId = externalId,
                Source = "GoogleBooks",
                Format = ContentFormat.Book,
                Title = bookData.volumeInfo.title,
                Description = bookData.volumeInfo.description,
                CoverUrl = bookData.volumeInfo.imageLinks?.thumbnail ?? bookData.volumeInfo.imageLinks?.smallThumbnail,
                Year = int.TryParse(bookData.volumeInfo.publishedDate?.Substring(0, 4), out var year) ? year : null,
                Rating = bookData.volumeInfo.averageRating ?? 0,
                Genres = bookData.volumeInfo.categories?.Where(c => !string.IsNullOrEmpty(c)).ToList(),
                Author = bookData.volumeInfo.authors?.FirstOrDefault(),
                Pages = bookData.volumeInfo.pageCount,
                IsFavorite = isFavorite,
                Trailers = null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка: {ex.Message}");
            return null;
        }
    }

    private async Task<List<TrailerDto>?> GetTrailersAsync(string externalId)
    {
        try
        {
            var url = $"{_kinopoiskConfig.BaseUrl}/{externalId}/videos";
            Console.WriteLine($" Запрос трейлеров: {url}");

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<TrailerDto>();

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($" JSON: {json.Substring(0, Math.Min(300, json.Length))}...");

            var videosData = JsonSerializer.Deserialize<KinopoiskVideosDto>(json);

            var trailers = videosData?.items
                ?.Where(v => !string.IsNullOrEmpty(v.url) &&
                            !string.IsNullOrEmpty(v.site) &&
                            v.site.ToLower().Contains("youtube"))
                ?.Select(v => new TrailerDto
                {
                    Title = v.name ?? "Трейлер",
                    YouTubeId = ExtractYouTubeId(v.url)
                })
                ?.Take(3)
                ?.ToList() ?? new List<TrailerDto>();

            Console.WriteLine($" Найдено трейлеров: {trailers.Count}");
            return trailers;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка трейлеров: {ex.Message}");
            return new List<TrailerDto>();
        }
    }

    private string ExtractYouTubeId(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";

        var patterns = new[]
        {
            @"/v/([a-zA-Z0-9_-]+)",
            @"v=([a-zA-Z0-9_-]+)",
            @"youtu\.be/([a-zA-Z0-9_-]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(url, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return "";
    }

    public async Task<List<ContentDetailDto>> GetSimilarContentAsync(string source, string externalId, int limit = 6)
    {
        if (source.ToLower() != "kinopoisk")
        {
            return new List<ContentDetailDto>();
        }

        try
        {
            var url = $"{_kinopoiskConfig.BaseUrl}/{externalId}/similar";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<ContentDetailDto>();

            var json = await response.Content.ReadAsStringAsync();
            var similarData = JsonSerializer.Deserialize<KinopoiskSimilarDto>(json);

            return similarData?.films
                ?.Take(limit)
                ?.Select(f => new ContentDetailDto
                {
                    ExternalId = f.filmId.ToString(),
                    Source = "Kinopoisk",
                    Title = f.nameRu ?? f.nameEn ?? "Без названия",
                    CoverUrl = f.posterUrl?.Replace("/300x450/", "/200x300/"),
                    Year = f.year,
                    Rating = f.ratingKinopoisk,
                    Format = f.type == "TV_SERIES" ? ContentFormat.Series : ContentFormat.Movie
                })
                ?.ToList() ?? new List<ContentDetailDto>();
        }
        catch
        {
            return new List<ContentDetailDto>();
        }
    }

    private ContentFormat DetermineFormat(KinopoiskFilmDto film)
    {
        var genres = film.genres?.Select(g => g.genre?.ToLower()).ToList() ?? new();
        var type = film.type?.ToUpper() ?? "";

        if (genres.Any(g => g?.Contains("аниме") == true || g?.Contains("мульт") == true))
        {
            return ContentFormat.Cartoon;
        }

        if (type.Contains("TV_SERIES") || type.Contains("MINI_SERIES"))
        {
            return ContentFormat.Series;
        }

        return ContentFormat.Movie;
    }

    private class KinopoiskFilmDto
    {
        public int filmId { get; set; }
        public string? nameRu { get; set; }
        public string? nameEn { get; set; }
        public string? nameOriginal { get; set; }
        public string? description { get; set; }
        public int? year { get; set; }
        public double? ratingKinopoisk { get; set; }
        public List<GenreItem>? genres { get; set; }
        public int? filmLength { get; set; }
        public string? posterUrl { get; set; }
        public string? posterUrlPreview { get; set; }
        public string? type { get; set; }
        public List<PersonItem>? directors { get; set; }
        public List<PersonItem>? actors { get; set; }
    }

    private class KinopoiskVideosDto
    {
        public List<VideoItem>? items { get; set; }
    }

    private class VideoItem
    {
        public string? name { get; set; }
        public string? site { get; set; }
        public string? url { get; set; }
    }

    private class KinopoiskSimilarDto
    {
        public List<SimilarItem>? films { get; set; }
    }

    private class SimilarItem
    {
        public int filmId { get; set; }
        public string? nameRu { get; set; }
        public string? nameEn { get; set; }
        public int? year { get; set; }
        public double? ratingKinopoisk { get; set; }
        public string? posterUrl { get; set; }
        public string? type { get; set; }
    }

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

    private class GenreItem
    {
        public string? genre { get; set; }
    }

    private class PersonItem
    {
        public string? name { get; set; }
    }
}
public interface IContentDetailService
{
    Task<ContentDetailDto?> GetContentDetailsAsync(string source, string externalId, string? userId = null);

    Task<List<ContentDetailDto>> GetSimilarContentAsync(string source, string externalId, int limit = 6);
}