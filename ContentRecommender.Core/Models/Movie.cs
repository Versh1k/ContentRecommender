namespace ContentRecommender.Core.Models;
public enum ContentTypeCategory
{
    FeatureFilm,   
    ShortFilm,     
    TvSeries,      
    MiniSeries,     
    Cartoon,    
}

public class Movie : ContentItem
{
    public Movie()
    {
        Format = ContentFormat.Movie;
    }

    public int? DurationMinutes { get; set; }
    public string? Director { get; set; }
    public List<string>? Actors { get; set; }
    public ContentTypeCategory Category { get; set; } = ContentTypeCategory.FeatureFilm;
}