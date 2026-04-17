using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using Microsoft.Extensions.Options;

namespace ContentRecommender.Web.Services.MovieSearch;

public class GenreMapper : IGenreMapper
{
    private readonly ApiAdapterConfig _config;

    public GenreMapper(IOptions<ApiAdapterConfig> options)
    {
        _config = options.Value;
    }

    private ProviderConfig Current => _config.Providers[_config.ActiveProvider];

    public int? GetGenreId(string genreName)
    {
        if (string.IsNullOrWhiteSpace(genreName))
            return null;

        var normalized = genreName.ToLowerInvariant().Trim();
        if (Current.GenreParameters.TryGetValue(normalized, out var idStr) && int.TryParse(idStr, out var id))
            return id;

        return null;
    }

    public List<int> GetGenreIdsForType(ContentTypeCategory type)
    {
        return new List<int>();
    }

    public int GetRandomGenreIdForType(ContentTypeCategory type, Random random)
    {
        var allIds = Current.GenreParameters.Values
            .Select(v => int.TryParse(v, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .ToList();

        if (allIds.Count == 0)
        {
            return 1;
        }

        return allIds[random.Next(allIds.Count)];
    }
}