using Microsoft.ML.Data;

namespace ContentRecommender.Web.ML.Models;

public class MoodInput
{
    [LoadColumn(0)]
    public string Text { get; set; } = "";

    [LoadColumn(1)]
    public uint Label { get; set; }
}

public class MoodPrediction
{
    [ColumnName("PredictedLabel")]
    public uint PredictedLabel { get; set; }

    [ColumnName("Score")]
    public float[] Scores { get; set; } = Array.Empty<float>();
}