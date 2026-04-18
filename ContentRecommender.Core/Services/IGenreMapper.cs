using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;

public interface IGenreMapper
{
    string? GetGenreParameter(string genreName, ContentFormat format);
}