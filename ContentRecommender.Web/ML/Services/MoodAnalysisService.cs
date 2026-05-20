using ContentRecommender.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ContentRecommender.Web.ML.Services;

public interface IMoodAnalysisService
{
    (string Mood, float Confidence) AnalyzeMoodWithConfidence(string text);
    string? AnalyzeMood(string text);
    (string Mood, float Confidence, float[] Scores) AnalyzeMoodFull(string text);
}

public class MoodAnalysisService : IMoodAnalysisService
{
    private readonly MLContext _mlContext;
    private readonly IWebHostEnvironment _env;
    private readonly MoodModelOptions _moodOptions;
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

    public MoodAnalysisService(MLContext mlContext, IWebHostEnvironment env, IOptions<MoodModelOptions> moodOptions)
    {
        _mlContext = mlContext;
        _env = env;
        _moodOptions = moodOptions.Value;
        LoadModel();
    }

    private void LoadModel()
    {
        try
        {
            var modelName = _moodOptions.ModelFileName
            ?? throw new InvalidOperationException("Не задано имя файла модели (MoodModel: ModelFileName) в конфигурации.");

            var possiblePaths = new[]
            {
                Path.Combine(_env.ContentRootPath, "ML", "Models", modelName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ML", "Models", modelName),
                Path.Combine(Directory.GetCurrentDirectory(), "ML", "Models", modelName)
            };

            string? modelPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    modelPath = path;
                    Console.WriteLine($"[ML] Модель найдена: {path}");
                    break;
                }
            }

            if (modelPath == null)
            {
                Console.WriteLine($"[ML] Модель не найдена. Имя: {modelName}");
                return;
            }

            _model = _mlContext.Model.Load(modelPath, out var schema);
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_model);
            Console.WriteLine("[ML] Модель загружена и готова к предсказаниям");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ML] Ошибка загрузки модели: {ex.Message}");
        }
    }

    public (string Mood, float Confidence) AnalyzeMoodWithConfidence(string text)
    {
        if (_predictionEngine == null || string.IsNullOrWhiteSpace(text))
            return (string.Empty, 0f);

        try
        {
            var prediction = _predictionEngine.Predict(new ModelInput { Text = text.Trim() });
            var confidence = prediction.Scores?.Length > 0 ? prediction.Scores[prediction.PredictedLabel] : 0f;
            string moodName = GetMoodName(prediction.PredictedLabel);
            return (moodName, confidence);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ML] Ошибка предсказания: {ex.Message}");
            return (string.Empty, 0f);
        }
    }

    public (string Mood, float Confidence, float[] Scores) AnalyzeMoodFull(string text)
    {
        if (_predictionEngine == null || string.IsNullOrWhiteSpace(text))
            return (string.Empty, 0f, Array.Empty<float>());

        var prediction = _predictionEngine.Predict(new ModelInput { Text = text.Trim() });
        
        var confidence = prediction.Scores?.Length > 0 ? prediction.Scores[prediction.PredictedLabel] : 0f;
        string moodName = GetMoodName(prediction.PredictedLabel);

        var softmaxScores = Softmax(prediction.Scores);

        return (moodName, confidence, softmaxScores);
    }

    public string? AnalyzeMood(string text)
    {
        var (mood, confidence) = AnalyzeMoodWithConfidence(text);
        return confidence > 0.1f ? mood : null;
    }

    private float[] Softmax(float[] scores)
    {
        if (scores == null || scores.Length == 0) return Array.Empty<float>();
        var max = scores.Max();
        var exp = scores.Select(x => (float)Math.Exp(x - max)).ToArray();
        var sum = exp.Sum();
        return sum > 0 ? exp.Select(x => x / sum).ToArray() : Array.Empty<float>();
    }

    private string GetMoodName(uint index)
    {
        var item = _moodOptions.Classes?.FirstOrDefault(c => c.Index == index);
        return item?.Name ?? string.Empty;
    }
}