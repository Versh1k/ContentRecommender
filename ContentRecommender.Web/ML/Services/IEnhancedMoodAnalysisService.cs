using ContentRecommender.Core.Models;

namespace ContentRecommender.Web.ML.Services;

public interface IEnhancedMoodAnalysisService
{
    MoodAnalysisResult AnalyzeMood(string text);
}