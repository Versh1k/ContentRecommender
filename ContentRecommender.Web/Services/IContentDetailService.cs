using ContentRecommender.Core.Models;

namespace ContentRecommender.Web.Services;
public interface IContentDetailService
{
    Task<ContentDetailDto?> GetContentDetailsAsync(string source, string externalId, string? userId = null);

    Task<List<ContentDetailDto>> GetSimilarContentAsync(string source, string externalId, int limit = 6);
}