public class MoodModelOptions
{
    public string DefaultCssClass { get; set; } = string.Empty;
    public List<MoodClassItem> Classes { get; set; } = new();
    public MlSettings MlSettings { get; set; } = new();
    public string? ModelFileName { get; set; }
}

public class MoodClassItem
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CssClass { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class MlSettings
{
    public string ModelFileName { get; set; } = string.Empty;
}