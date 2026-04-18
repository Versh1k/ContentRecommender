using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;

public interface IBookDetailService
{
    Task<BookDetailDto?> GetBookDetailsAsync(string externalId);
    Task<List<BookSummaryDto>> GetSimilarBooksAsync(string externalId, int limit = 6);
}