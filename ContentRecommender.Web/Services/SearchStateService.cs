using ContentRecommender.Core.Models;
using Microsoft.JSInterop;
using static ContentRecommender.Web.Services.SearchStateService;

namespace ContentRecommender.Web.Services;

public interface ISearchStateService
{
    Task SaveSearchStateAsync(string query, List<string> genres, MoodType? mood, SearchCriteria.SearchContentType contentType, List<ContentItem> results);
    Task<SearchState?> GetSearchStateAsync();
    Task ClearSearchStateAsync();
}

public class SearchStateService : ISearchStateService
{
    private readonly IJSRuntime _js;
    private SearchState? _currentState;

    public SearchStateService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SaveSearchStateAsync(string query, List<string> genres, MoodType? mood, SearchCriteria.SearchContentType contentType, List<ContentItem> results)
    {
        try
        {
            _currentState = new SearchState
            {
                Query = query,
                Genres = genres,
                Mood = mood?.ToString(),
                ContentType = contentType.ToString(),
                Timestamp = DateTime.UtcNow.Ticks,
                SavedResults = results.Select(r => new SavedResult
                {
                    ExternalId = r.ExternalId,
                    Source = r.Source,
                    Title = r.Title,
                    CoverUrl = r.CoverUrl,
                    Year = r.Year,
                    Rating = r.Rating,
                    Format = r.Format
                }).Take(20).ToList()
            };

            await _js.InvokeVoidAsync("localStorage.setItem", "searchState",
                System.Text.Json.JsonSerializer.Serialize(_currentState));
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка сохранения состояния: {ex.Message}");
        }
    }

    public async Task<SearchState?> GetSearchStateAsync()
    {
        try
        {
            if (_currentState != null)
                return _currentState;

            var json = await _js.InvokeAsync<string>("localStorage.getItem", "searchState");

            if (string.IsNullOrEmpty(json))
                return null;

            _currentState = System.Text.Json.JsonSerializer.Deserialize<SearchState>(json);
            return _currentState;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка загрузки состояния: {ex.Message}");
            return null;
        }
    }

    public async Task ClearSearchStateAsync()
    {
        try
        {
            _currentState = null;
            await _js.InvokeVoidAsync("localStorage.removeItem", "searchState");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка очистки состояния: {ex.Message}");
        }
    }

    public class SearchState
    {
        public string Query { get; set; } = "";
        public List<string> Genres { get; set; } = new();
        public string? Mood { get; set; }
        public string ContentType { get; set; } = "";
        public long Timestamp { get; set; }
        public List<SavedResult>? SavedResults { get; set; }
    }

    public class SavedResult
    {
        public string ExternalId { get; set; } = "";
        public string Source { get; set; } = "";
        public string Title { get; set; } = "";
        public string? CoverUrl { get; set; }
        public int? Year { get; set; }
        public double? Rating { get; set; }
        public ContentFormat Format { get; set; }
    }
}