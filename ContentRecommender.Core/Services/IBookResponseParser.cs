using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Services;

public interface IBookResponseParser
{
    List<Book> Parse(string json);
}