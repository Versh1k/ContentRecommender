using ContentRecommender.Core.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ContentRecommender.Web.ML.Services;

public interface IMoodAnalysisService
{
    (MoodType Mood, float Confidence) AnalyzeMoodWithConfidence(string text);
    MoodType? AnalyzeMood(string text);
    (MoodType Mood, float Confidence, float[] Scores) AnalyzeMoodFull(string text);
}

public class MoodAnalysisService : IMoodAnalysisService
{
    private readonly MLContext _mlContext;
    private readonly IWebHostEnvironment _env;
    private ITransformer? _model;
    private PredictionEngine<ModelInput, ModelOutput>? _predictionEngine;
    private class ModelInput
    {
        [LoadColumn(0)]
        public string Text { get; set; } = "";
        [ColumnName("Label")]
        public uint Label { get; set; } = 0;
    }

    private class ModelOutput
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedLabel { get; set; }
        [ColumnName("Score")]
        public float[] Scores { get; set; } = Array.Empty<float>();
    }

    public MoodAnalysisService(MLContext mlContext, IWebHostEnvironment env)
    {
        _mlContext = mlContext;
        _env = env;
        LoadModel();
    }

    private void LoadModel()
    {
        try
        {
            var possiblePaths = new[]
            {
            Path.Combine(_env.ContentRootPath, "ML", "Models", "MoodAnalyzer(700prim_na_class).zip"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ML", "Models", "MoodAnalyzer(700prim_na_class).zip"),
            Path.Combine(Directory.GetCurrentDirectory(), "ML", "Models", "MoodAnalyzer(700prim_na_class).zip")
        };

            string? modelPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    modelPath = path;
                    break;
                }
            }

            if (modelPath == null)
            {
                Console.WriteLine(" Модель не найдена");
                return;
            }
            _model = _mlContext.Model.Load(modelPath, out var schema);
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_model);

            Console.WriteLine($" Модель загружена и готова к предсказаниям");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка: {ex.Message}");
        }
    }

    public (MoodType Mood, float Confidence) AnalyzeMoodWithConfidence(string text)
    {
        if (_predictionEngine == null || string.IsNullOrWhiteSpace(text))
        {
            return (MoodType.Everyday, 0f);
        }

        try
        {
            var prediction = _predictionEngine.Predict(new ModelInput { Text = text });
            var confidence = prediction.Scores?.Length > 0? prediction.Scores[prediction.PredictedLabel] : 0f;

            return ((MoodType)prediction.PredictedLabel, confidence);
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка предсказания: {ex.Message}");
            return (MoodType.Everyday, 0f);
        }
    }
    private float[] Softmax(float[] scores)
    {
        var max = scores.Max();
        var exp = scores.Select(x => (float)Math.Exp(x - max)).ToArray();
        var sum = exp.Sum();
        return exp.Select(x => x / sum).ToArray();
    }

    public (MoodType Mood, float Confidence, float[] Scores) AnalyzeMoodFull(string text)
    {
        if (_predictionEngine == null || string.IsNullOrWhiteSpace(text))
            return (MoodType.Everyday, 0f, Array.Empty<float>());

        var prediction = _predictionEngine.Predict(new ModelInput { Text = text });
        var softmaxScores = Softmax(prediction.Scores);
        var maxIndex = Array.IndexOf(softmaxScores, softmaxScores.Max());
        var confidence = softmaxScores[maxIndex];
        return ((MoodType)maxIndex, confidence, softmaxScores);
    }
    public MoodType? AnalyzeMood(string text)
    {
        var result = AnalyzeMoodWithConfidence(text);
        return result.Confidence > 0.1f ? result.Mood : (MoodType?)null;
    }
}