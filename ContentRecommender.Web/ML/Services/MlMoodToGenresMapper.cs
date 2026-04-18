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
    private readonly MovieApiOptions _options;

    public MlMoodToGenresMapper(IOptions<MovieApiOptions> options)
    {
        _options = options.Value;
    }

    public List<string> GetGenresFromMoodScores(MoodType predictedMood, float[] scores, float threshold = 0.2f)
    {
        var provider = _options.Providers[_options.ActiveProvider];
        var moodToGenres = provider.MoodToGenres;

        var resultGenres = new List<string>();
        var moodKey = predictedMood.ToString();
        if (moodToGenres.TryGetValue(moodKey, out var primaryGenres))
            resultGenres.AddRange(primaryGenres);

        return resultGenres.Distinct().ToList();
    }
}