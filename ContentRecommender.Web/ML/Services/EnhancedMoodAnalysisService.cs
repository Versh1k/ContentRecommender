using ContentRecommender.Core.Models;
using Microsoft.ML;

namespace ContentRecommender.Web.ML.Services;

public class EnhancedMoodAnalysisService : IEnhancedMoodAnalysisService
{
    private readonly IMoodAnalysisService _mlService;
    private readonly Dictionary<MoodType, List<string>> _keywords;
    private readonly HashSet<string> _emotionWords;

    public EnhancedMoodAnalysisService(IMoodAnalysisService mlService)
    {
        _mlService = mlService;

        _keywords = new()
        {
            [MoodType.Romantic] = new() { "романт", "любов", "нежн", "чувств", "свидан", "сердц", "влюб" },
            [MoodType.Mysterious] = new() { "загадк", "тайн", "мистик", "детектив", "секрет", "расслед", "сыщик" },
            [MoodType.Sad] = new() { "груст", "печаль", "трагед", "слёз", "тоск", "горе", "потеря" },
            [MoodType.Funny] = new() { "весел", "смешн", "юмор", "комед", "забав", "лёгк", "позитив", "ирон" },
            [MoodType.Crime] = new() { "криминал", "бандит", "мафи", "ограбл", "полиция", "вор", "преступ" },
            [MoodType.Military] = new() { "воен", "арми", "фронт", "солдат", "битв", "подвиг", "войн" },
            [MoodType.Everyday] = new() { "спокой", "тих", "повседнев", "обычн", "семья", "дом", "уют" },
            [MoodType.Tense] = new() { "напряж", "страх", "триллер", "тревог", "погон", "риск", "адреналин" },
            [MoodType.Horror] = new() { "ужас", "страшн", "кошмар", "монстр", "призрак", "жуть", "хоррор" },
            [MoodType.Inspiring] = new() { "вдохнов", "мотив", "успех", "побед", "мечт", "достиж", "верь" },
            [MoodType.Epic] = new() { "эпичн", "велик", "герой", "масштаб", "фэнтези", "дракон", "маги" },
            [MoodType.Adventure] = new() { "приключ", "путешеств", "экспедиц", "сокровищ", "джунгл", "поиск" }
        };

        _emotionWords = new()
        {
            "весёлый", "весёлое", "смешной", "юмор", "комедия", "ирония",
            "грустный", "грустное", "печальный", "романтичный", "романтика", "любовь",
            "страшный", "ужас", "напряжённый", "загадочный", "эпичный", "масштабный",
            "вдохновляющий", "мотивация", "спокойный", "тихий", "лёгкий", "уютный",
            "криминальный", "военный", "приключения", "фэнтези", "дракон"
        };
    }

    public MoodAnalysisResult AnalyzeMood(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new MoodAnalysisResult { Mood = MoodType.Everyday, Confidence = 0 };

        var lowerText = text.ToLowerInvariant();
        var keywordResult = AnalyzeWithKeywords(lowerText);
        bool hasEmotionWords = _emotionWords.Any(w => lowerText.Contains(w));

        if (!hasEmotionWords)
        {
            return new MoodAnalysisResult
            {
                Mood = keywordResult.Mood,
                Confidence = keywordResult.Confidence,
                UsedMlModel = false,
                DetectedKeywords = keywordResult.DetectedKeywords,
                IsSearchQuery = true
            };
        }

        var (mlMood, mlConfidence) = _mlService.AnalyzeMoodWithConfidence(text);

        if (mlConfidence >= 0.3f)
        {
            return new MoodAnalysisResult
            {
                Mood = mlMood,
                Confidence = mlConfidence,
                UsedMlModel = true,
                DetectedKeywords = keywordResult.DetectedKeywords,
                IsSearchQuery = false
            };
        }

        return new MoodAnalysisResult
        {
            Mood = keywordResult.Mood,
            Confidence = keywordResult.Confidence,
            UsedMlModel = false,
            DetectedKeywords = keywordResult.DetectedKeywords,
            IsSearchQuery = false
        };
    }

    private MoodAnalysisResult AnalyzeWithKeywords(string text)
    {
        var scores = new Dictionary<MoodType, int>();
        var detectedKeywords = new List<string>();

        foreach (var kv in _keywords)
        {
            foreach (var keyword in kv.Value)
            {
                if (text.Contains(keyword))
                {
                    scores[kv.Key] = scores.GetValueOrDefault(kv.Key, 0) + 1;
                    detectedKeywords.Add(keyword);
                }
            }
        }

        if (scores.Any())
        {
            var bestMatch = scores.OrderByDescending(x => x.Value).First();
            var confidence = Math.Min(0.7f, (float)bestMatch.Value / 15f + 0.2f);

            return new MoodAnalysisResult
            {
                Mood = bestMatch.Key,
                Confidence = confidence,
                UsedMlModel = false,
                DetectedKeywords = detectedKeywords.Distinct().Take(5).ToList()
            };
        }

        return new MoodAnalysisResult
        {
            Mood = MoodType.Everyday,
            Confidence = 0.2f,
            UsedMlModel = false
        };
    }
}

public class MoodAnalysisResult
{
    public MoodType Mood { get; set; }
    public float Confidence { get; set; }
    public bool UsedMlModel { get; set; }
    public List<string> DetectedKeywords { get; set; } = new();
    public bool IsSearchQuery { get; set; }
}