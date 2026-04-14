using ContentRecommender.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ContentRecommender.Web.ML.Services;

public static class GenreMapper
{
    /// <summary>
    /// Базовые жанры, ассоциированные с каждым типом настроения
    /// </summary>
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

    /// <summary>
    /// Формирует список жанров на основе предсказанного настроения и распределения уверенности по всем настроениям.
    /// </summary>
    /// <param name="predictedMood">Основное предсказанное настроение</param>
    /// <param name="scores">Массив уверенности для каждого MoodType (индекс = (int)MoodType)</param>
    /// <param name="threshold">Порог разницы между первым и вторым по величине скором для добавления жанров второго настроения</param>
    /// <returns>Список жанров (без дубликатов)</returns>
    public static List<string> GetGenresFromMoodScores(MoodType predictedMood, float[] scores, float threshold = 0.2f)
    {
        if (scores == null || scores.Length == 0)
            return new List<string>();

        // Сортируем настроения по убыванию уверенности
        var sorted = scores
            .Select((score, idx) => (Mood: (MoodType)idx, Score: score))
            .OrderByDescending(x => x.Score)
            .ToList();

        var resultGenres = new List<string>();

        // Всегда добавляем жанры основного настроения
        if (MoodBaseGenres.TryGetValue(predictedMood, out var primaryGenres))
            resultGenres.AddRange(primaryGenres);

        // Если второе настроение достаточно близко по уверенности, добавляем его жанры
        if (sorted.Count >= 2)
        {
            var top = sorted[0];
            var second = sorted[1];

            // Добавляем жанры второго настроения, если:
            // - первое настроение совпадает с predictedMood, и разница меньше порога
            // - или первое настроение не совпадает с predictedMood (редкий случай, но обрабатываем)
            if (top.Mood == predictedMood && top.Score - second.Score < threshold)
            {
                if (MoodBaseGenres.TryGetValue(second.Mood, out var secondaryGenres))
                    resultGenres.AddRange(secondaryGenres);
            }
            else if (top.Mood != predictedMood)
            {
                // Если ML ошибся с основным, но мы всё равно доверяем первому результату
                if (MoodBaseGenres.TryGetValue(top.Mood, out var altGenres))
                    resultGenres.AddRange(altGenres);
            }
        }

        return resultGenres.Distinct().ToList();
    }
}