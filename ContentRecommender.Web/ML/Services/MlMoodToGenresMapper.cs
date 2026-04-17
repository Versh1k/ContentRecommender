using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using Microsoft.Extensions.Options;

namespace ContentRecommender.Web.ML.Services;

public interface IMlGenreMapper
{
    List<string> GetGenresFromMoodScores(MoodType predictedMood, float[] scores, float threshold = 0.2f);
}

public class MlMoodToGenresMapper : IMlGenreMapper
{
    private readonly ApiAdapterConfig _apiConfig;

    public MlMoodToGenresMapper(IOptions<ApiAdapterConfig> options)
    {
        _apiConfig = options.Value;
    }

    public List<string> GetGenresFromMoodScores(MoodType predictedMood, float[] scores, float threshold = 0.2f)
    {
        var provider = _apiConfig.Providers[_apiConfig.ActiveProvider];
        var moodToGenres = provider.MoodToGenres;

        var resultGenres = new List<string>();

        var moodKey = predictedMood.ToString();
        if (moodToGenres.TryGetValue(moodKey, out var primaryGenres))
        {
            resultGenres.AddRange(primaryGenres);
        }

        return resultGenres.Distinct().ToList();
    }
}