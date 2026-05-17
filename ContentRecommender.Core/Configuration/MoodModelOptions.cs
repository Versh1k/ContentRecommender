public class MoodModelOptions
{
    public List<MoodClassItem> Classes { get; set; } = new();
}

public class MoodClassItem
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public string CssClass { get; set; } = "";
    public string DisplayName { get; set; } = "";
}