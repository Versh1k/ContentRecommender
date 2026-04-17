using ContentRecommender.Core.Models;

namespace ContentRecommender.Core.Helpers;

public static class MoodHelper
{
    public static string GetMoodClass(MoodType mood) => mood switch
    {
        MoodType.Romantic => "mood-romantic",
        MoodType.Mysterious => "mood-mysterious",
        MoodType.Sad => "mood-sad",
        MoodType.Funny => "mood-funny",
        MoodType.Crime => "mood-crime",
        MoodType.Military => "mood-military",
        MoodType.Everyday => "mood-everyday",
        MoodType.Tense => "mood-tense",
        MoodType.Horror => "mood-horror",
        MoodType.Inspiring => "mood-inspiring",
        MoodType.Epic => "mood-epic",
        MoodType.Adventure => "mood-adventure",
        _ => "mood-everyday"
    };

    public static string GetMoodDisplayName(MoodType mood) => mood switch
    {
        MoodType.Romantic => "Романтичное",
        MoodType.Mysterious => "Загадочное",
        MoodType.Sad => "Грустное",
        MoodType.Funny => "Весёлое",
        MoodType.Crime => "Криминальное",
        MoodType.Military => "Военное",
        MoodType.Everyday => "Повседневное",
        MoodType.Tense => "Напряжённое",
        MoodType.Horror => "Страшное",
        MoodType.Inspiring => "Вдохновляющее",
        MoodType.Epic => "Эпичное",
        MoodType.Adventure => "Приключенческое",
        _ => "Неопределено"
    };
}