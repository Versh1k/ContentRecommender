using ContentRecommender.Core.Models;

namespace ContentRecommender.Web.ML.Services;

public static class MoodMapper
{
    private static readonly Dictionary<int, MoodType> _directMapping = new()
    {
        [0] = MoodType.Romantic,
        [1] = MoodType.Mysterious,
        [2] = MoodType.Sad,
        [3] = MoodType.Funny,
        [4] = MoodType.Crime,
        [5] = MoodType.Military,
        [6] = MoodType.Everyday,
        [7] = MoodType.Tense,
        [8] = MoodType.Horror,
        [9] = MoodType.Inspiring,
        [10] = MoodType.Epic,
        [11] = MoodType.Adventure
    };

    public static MoodType MapToMood(uint label)
    {
        int key = (int)label;

        if (_directMapping.TryGetValue(key, out var directMood))
        {
            return directMood;
        }

        return MoodType.Everyday;
    }
}