using ContentRecommender.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ContentRecommender.Web.ML.Services;

public static class GenreMapper
{
    public static readonly Dictionary<MoodType, List<string>> MoodBaseGenres = new()
    {
        { MoodType.Romantic,   new() { "мелодрама", "романтика" } },
        { MoodType.Mysterious, new() { "детектив", "триллер" } },
        { MoodType.Sad,        new() { "драма", "биография" } },
        { MoodType.Funny,      new() { "комедия", "семейный" } },
        { MoodType.Crime,      new() { "криминал", "детектив" } },
        { MoodType.Military,   new() { "военный", "исторический" } },
        { MoodType.Everyday,   new() { "документальный", "семейный" } },
        { MoodType.Tense,      new() { "триллер", "боевик" } },
        { MoodType.Horror,     new() { "ужасы", "мистика" } },
        { MoodType.Inspiring,  new() { "биография", "спортивный" } },
        { MoodType.Epic,       new() { "фэнтези", "приключения" } },
        { MoodType.Adventure,  new() { "приключения", "боевик" } }
    };

    public static List<string> GetGenresFromMoodScores(MoodType predictedMood, float[] scores, float threshold = 0.2f)
    {
        if (scores == null || scores.Length == 0)
            return new List<string>();

        ///ТОП 2 жанр
        //var sorted = scores
        //    .Select((score, idx) => (Mood: (MoodType)idx, Score: score))
        //    .OrderByDescending(x => x.Score)
        //    .ToList();

        var resultGenres = new List<string>();

        if (MoodBaseGenres.TryGetValue(predictedMood, out var primaryGenres))
            resultGenres.AddRange(primaryGenres);

        //if (sorted.Count >= 2)
        //{
        //    var top = sorted[0];
        //    var second = sorted[1];

        //    if (top.Mood == predictedMood && top.Score - second.Score < threshold)
        //    {
        //        if (MoodBaseGenres.TryGetValue(second.Mood, out var secondaryGenres))
        //            resultGenres.AddRange(secondaryGenres);
        //    }
        //    else if (top.Mood != predictedMood)
        //    {
        //        if (MoodBaseGenres.TryGetValue(top.Mood, out var altGenres))
        //            resultGenres.AddRange(altGenres);
        //    }
        //}

        return resultGenres.Distinct().ToList();
    }
}