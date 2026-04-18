using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using ContentRecommender.Data.Repositories;
using Microsoft.Extensions.Options;

namespace ContentRecommender.Web.Services;

public class ContentDetailService : IContentDetailService
{
    private readonly IMovieDetailService _movieDetail;
    private readonly IBookDetailService _bookDetail;
    private readonly IFavoritesRepository _favorites;
    private readonly MovieApiOptions _movieOptions;
    private readonly BookApiOptions _bookOptions;

    public ContentDetailService(IMovieDetailService movieDetail,
                                IBookDetailService bookDetail,
                                IFavoritesRepository favorites,
                                IOptions<MovieApiOptions> movieOptions,
                                IOptions<BookApiOptions> bookOptions)
    {
        _movieDetail = movieDetail;
        _bookDetail = bookDetail;
        _favorites = favorites;
        _movieOptions = movieOptions.Value;
        _bookOptions = bookOptions.Value;
    }

    public async Task<ContentDetailDto?> GetContentDetailsAsync(string source, string externalId, string? userId = null)
    {
        if (source == _movieOptions.ActiveProvider)
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
                Trailers = movie.Trailers?.Select(v => new TrailerDto
                { Title = v.Title, YouTubeId = v.YouTubeId }).ToList(),
                IsFavorite = !string.IsNullOrEmpty(userId) &&
                    await _favorites.IsFavoriteAsync(userId, externalId, movie.Source)
            };
        }
        else if (source == _bookOptions.ActiveProvider)
        {
            var book = await _bookDetail.GetBookDetailsAsync(externalId);
            if (book == null) return null;

            return new ContentDetailDto
            {
                ExternalId = book.ExternalId,
                Source = book.Source,
                Format = ContentFormat.Book,
                Title = book.Title,
                Description = book.Description,
                CoverUrl = book.CoverUrl,
                Year = book.Year,
                Rating = book.Rating,
                Genres = book.Genres,
                Author = book.Author,
                Pages = book.Pages,
                IsFavorite = !string.IsNullOrEmpty(userId) &&
                    await _favorites.IsFavoriteAsync(userId, externalId, book.Source)
            };
        }

        return null;
    }

    public async Task<List<ContentDetailDto>> GetSimilarContentAsync(string source, string externalId, int limit = 6)
    {
        if (source == _movieOptions.ActiveProvider)
        {
            var similar = await _movieDetail.GetSimilarMoviesAsync(externalId, limit);
            return similar.Select(s => new ContentDetailDto
            {
                ExternalId = s.ExternalId,
                Source = source,
                Title = s.Title,
                CoverUrl = s.CoverUrl,
                Year = s.Year,
                Rating = s.Rating,
                Format = s.Format
            }).ToList();
        }

        return new List<ContentDetailDto>();
    }
}