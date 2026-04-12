using Microsoft.ML.Data;

public class MoodInput
{
    [LoadColumn(0)]
    public string Text { get; set; } = "";

    [LoadColumn(1)]
    public uint Label { get; set; }
}