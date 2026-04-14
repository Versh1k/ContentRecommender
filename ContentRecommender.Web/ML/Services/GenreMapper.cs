using ContentRecommender.Core.Models;

namespace ContentRecommender.Web.ML.Services;

public static class GenreMapper
{
    public static readonly Dictionary<MoodType, List<string>> MoodBaseGenres = new()
    {
        { MoodType.Romantic, new() { "мелодрама", "романтика" } },
        { MoodType.Mysterious, new() { "детектив", "триллер" } },
        { MoodType.Sad, new() { "драма", "биография" } },
        { MoodType.Funny, new() { "комедия", "семейный" } },
        { MoodType.Crime, new() { "криминал", "детектив" } },
        { MoodType.Military, new() { "военный", "исторический" } },
        { MoodType.Everyday, new() { "документальный", "семейный" } },
        { MoodType.Tense, new() { "триллер", "боевик" } },
        { MoodType.Horror, new() { "ужасы", "мистика" } },
        { MoodType.Inspiring, new() { "биография", "спортивный" } },
        { MoodType.Epic, new() { "фэнтези", "приключения" } },
        { MoodType.Adventure, new() { "приключения", "боевик" } }
    };

    // Компактная версия GetGenresFromMoodScores
    public static List<string> GetGenresFromMoodScores(MoodType predictedMood, float[] scores, float threshold = 0.2f)
    {
        if (scores == null || scores.Length == 0) return new List<string>();

        var topTwo = scores.Select((s, i) => (Mood: (MoodType)i, Score: s))
                           .OrderByDescending(x => x.Score).Take(2).ToList();
        var genres = new List<string>(MoodBaseGenres[predictedMood]);

        if (topTwo[0].Mood == predictedMood && topTwo.Count > 1 && topTwo[0].Score - topTwo[1].Score < threshold)
            genres.AddRange(MoodBaseGenres[topTwo[1].Mood]);
        else if (topTwo[0].Mood != predictedMood)
            genres.AddRange(MoodBaseGenres[topTwo[0].Mood]);

        return genres.Distinct().ToList();
    }
    private static readonly Dictionary<string, List<string>> KeywordToGenres = new(StringComparer.InvariantCultureIgnoreCase)
    {
        // Romantic
        { "любов", new() { "мелодрама", "романтика" } },
        { "роман", new() { "мелодрама", "романтика" } },
        { "страст", new() { "мелодрама", "романтика" } },
        { "нежн", new() { "мелодрама", "романтика" } },
        { "свидан", new() { "мелодрама", "романтика" } },
        { "сердц", new() { "мелодрама", "романтика" } },
        { "влюб", new() { "мелодрама", "романтика" } },
        { "чувств", new() { "мелодрама", "романтика" } },

        // Mysterious
        { "загадк", new() { "детектив", "триллер", "мистика" } },
        { "тайн", new() { "детектив", "триллер", "мистика" } },
        { "мистик", new() { "мистика", "детектив" } },
        { "детектив", new() { "детектив", "триллер" } },
        { "расслед", new() { "детектив", "криминал" } },
        { "сыщик", new() { "детектив" } },

        // Sad
        { "груст", new() { "драма", "мелодрама" } },
        { "печаль", new() { "драма" } },
        { "трагед", new() { "драма" } },
        { "слёз", new() { "драма", "мелодрама" } },
        { "тоск", new() { "драма" } },
        { "потер", new() { "драма" } },

        // Funny
        { "смешн", new() { "комедия" } },
        { "юмор", new() { "комедия" } },
        { "комед", new() { "комедия" } },
        { "весел", new() { "комедия", "семейный" } },
        { "забав", new() { "комедия" } },
        { "шутк", new() { "комедия" } },
        { "ирон", new() { "комедия" } },

        // Crime
        { "криминал", new() { "криминал", "детектив" } },
        { "бандит", new() { "криминал", "боевик" } },
        { "мафи", new() { "криминал" } },
        { "ограбл", new() { "криминал", "боевик" } },
        { "преступ", new() { "криминал", "детектив" } },
        { "полиц", new() { "детектив", "криминал" } },

        // Military
        { "воен", new() { "военный", "исторический" } },
        { "арми", new() { "военный" } },
        { "солдат", new() { "военный", "исторический" } },
        { "фронт", new() { "военный" } },
        { "битв", new() { "военный", "исторический" } },
        { "подвиг", new() { "военный", "исторический" } },
        { "войн", new() { "военный" } },

        // Everyday
        { "спокой", new() { "документальный", "семейный" } },
        { "повседнев", new() { "документальный", "семейный" } },
        { "обычн", new() { "документальный", "семейный" } },
        { "семья", new() { "семейный", "драма" } },
        { "уют", new() { "документальный", "семейный" } },

        // Tense
        { "напряж", new() { "триллер", "боевик" } },
        { "триллер", new() { "триллер" } },
        { "погон", new() { "боевик", "триллер" } },
        { "адреналин", new() { "боевик", "триллер" } },
        { "риск", new() { "боевик", "триллер" } },

        // Horror
        { "ужас", new() { "ужасы", "мистика" } },
        { "страшн", new() { "ужасы" } },
        { "кошмар", new() { "ужасы" } },
        { "монстр", new() { "ужасы", "фэнтези" } },
        { "призрак", new() { "ужасы", "мистика" } },
        { "хоррор", new() { "ужасы" } },

        // Inspiring
        { "вдохнов", new() { "биография", "спортивный" } },
        { "мотив", new() { "спортивный", "биография" } },
        { "успех", new() { "биография", "спортивный" } },
        { "побед", new() { "спортивный", "биография" } },
        { "мечт", new() { "биография", "драма" } },

        // Epic
        { "эпичн", new() { "фэнтези", "приключения" } },
        { "фэнтези", new() { "фэнтези", "приключения" } },
        { "маги", new() { "фэнтези" } },
        { "дракон", new() { "фэнтези" } },
        { "герой", new() { "фэнтези", "приключения" } },

        // Adventure
        { "приключ", new() { "приключения", "боевик" } },
        { "путешеств", new() { "приключения" } },
        { "экспедиц", new() { "приключения" } },
        { "сокровищ", new() { "приключения" } },
        { "джунгл", new() { "приключения" } },
        { "поиск", new() { "приключения" } }
    };

    public static List<string> ExtractGenresByKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        var lower = text.ToLowerInvariant();
        var result = new HashSet<string>();
        foreach (var kv in KeywordToGenres)
            if (lower.Contains(kv.Key))
                foreach (var g in kv.Value) result.Add(g);
        return result.ToList();
    }
}