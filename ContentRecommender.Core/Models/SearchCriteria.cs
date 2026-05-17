using System;
using System.Collections.Generic;

namespace ContentRecommender.Core.Models;

public class SearchCriteria
{
    public string? UserInput { get; set; }
    public string? Mood { get; set; }
    public int? AvailableTimeMinutes { get; set; }
    public List<ContentFormat>? ContentTypes { get; set; }
    public bool? OnlyCompleted { get; set; }
    public List<string>? Genres { get; set; }
    public int? MinRating { get; set; }
    public string? Language { get; set; }

    public SearchContentType SelectedContentType { get; set; } = SearchContentType.All;
    public Guid? RandomSeed { get; set; }

    public enum SearchContentType
    {
        All,
        Movies,
        TvSeries,
        Cartoons,
        AllBooks
    }
}