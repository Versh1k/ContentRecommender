using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using ContentRecommender.Data;
using ContentRecommender.Data.Repositories;
using ContentRecommender.Web.Components;
using ContentRecommender.Web.ML.Services;
using ContentRecommender.Web.Services;
using ContentRecommender.Web.Services.BookSearch;
using ContentRecommender.Web.Services.MovieSearch;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var jsonPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.json");
if (!File.Exists(jsonPath))
    throw new FileNotFoundException("appsettings.json not found", jsonPath);

var jsonString = File.ReadAllText(jsonPath);
var fullConfig = JsonSerializer.Deserialize<AppSettingsRoot>(jsonString)
    ?? throw new InvalidOperationException("Failed to deserialize appsettings.json");

var movieOptions = fullConfig.MovieApi;
var bookOptions = fullConfig.BookApi;

if (movieOptions == null)
    throw new InvalidOperationException("Секция 'MovieApi' не найдена или повреждена");
if (bookOptions == null)
    throw new InvalidOperationException("Секция 'BookApi' не найдена или повреждена");

if (!movieOptions.Providers.ContainsKey(movieOptions.ActiveProvider))
    throw new InvalidOperationException($"Провайдер фильмов '{movieOptions.ActiveProvider}' не найден. Доступные: {string.Join(", ", movieOptions.Providers.Keys)}");
if (!bookOptions.Providers.ContainsKey(bookOptions.ActiveProvider))
    throw new InvalidOperationException($"Провайдер книг '{bookOptions.ActiveProvider}' не найден. Доступные: {string.Join(", ", bookOptions.Providers.Keys)}");

builder.Services.AddSingleton(movieOptions);
builder.Services.AddSingleton(bookOptions);
builder.Services.AddSingleton<IOptions<MovieApiOptions>>(new OptionsWrapper<MovieApiOptions>(movieOptions));
builder.Services.AddSingleton<IOptions<BookApiOptions>>(new OptionsWrapper<BookApiOptions>(bookOptions));

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

builder.Services.Configure<MoodModelOptions>(builder.Configuration.GetSection("MoodModel"));
builder.Services.Configure<SearchSettings>(
    builder.Configuration.GetSection("SearchSettings"));

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var cacheSettings = builder.Configuration.GetSection("CacheSettings").Get<CacheSettings>()
    ?? new CacheSettings { DurationDays = 7, MaxItemsPerCategory = 15 };
builder.Services.AddSingleton(cacheSettings);

builder.Services.AddScoped<IFavoritesRepository, FavoritesRepository>();

builder.Services.AddSingleton<MLContext>();
builder.Services.AddScoped<IMoodAnalysisService, MoodAnalysisService>();

builder.Services.AddScoped<ContentSearchService>();
builder.Services.AddScoped<IFavoritesService, FavoritesService>();
builder.Services.AddScoped<ISearchStateService, SearchStateService>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IContentCacheService, ContentCacheService>();

// Фильмы
builder.Services.AddHttpClient<IMovieSearchService, GenericMovieSearchService>((sp, client) =>
{
    var options = sp.GetRequiredService<MovieApiOptions>();
    var provider = options.Providers[options.ActiveProvider];
    client.BaseAddress = new Uri(provider.BaseUrl);
    if (!string.IsNullOrEmpty(provider.ApiKeyHeader) && !string.IsNullOrEmpty(provider.ApiKey))
        client.DefaultRequestHeaders.Add(provider.ApiKeyHeader, provider.ApiKey);
});

builder.Services.AddHttpClient<IMovieDetailService, GenericMovieDetailService>((sp, client) =>
{
    var options = sp.GetRequiredService<MovieApiOptions>();
    var provider = options.Providers[options.ActiveProvider];
    client.BaseAddress = new Uri(provider.BaseUrl);
    if (!string.IsNullOrEmpty(provider.ApiKeyHeader) && !string.IsNullOrEmpty(provider.ApiKey))
        client.DefaultRequestHeaders.Add(provider.ApiKeyHeader, provider.ApiKey);
});

// Книги
builder.Services.AddHttpClient<IBookSearchService, GenericBookSearchService>((sp, client) =>
{
    var options = sp.GetRequiredService<BookApiOptions>();
    var provider = options.Providers[options.ActiveProvider];
    client.BaseAddress = new Uri(provider.BaseUrl);

    client.DefaultRequestHeaders.Add("User-Agent", "ContentRecommender/1.0");

    if (!string.IsNullOrEmpty(provider.ApiKeyHeader) && !string.IsNullOrEmpty(provider.ApiKey))
        client.DefaultRequestHeaders.Add(provider.ApiKeyHeader, provider.ApiKey);
});

builder.Services.AddHttpClient<IBookDetailService, GenericBookDetailService>((sp, client) =>
{
    var options = sp.GetRequiredService<BookApiOptions>();
    var provider = options.Providers[options.ActiveProvider];
    client.BaseAddress = new Uri(provider.BaseUrl);

    client.DefaultRequestHeaders.Add("User-Agent", "ContentRecommender/1.0");

    if (!string.IsNullOrEmpty(provider.ApiKeyHeader) && !string.IsNullOrEmpty(provider.ApiKey))
        client.DefaultRequestHeaders.Add(provider.ApiKeyHeader, provider.ApiKey);
});

// Компоненты
builder.Services.AddScoped<IMovieApiUrlBuilder, MovieApiUrlBuilder>();
builder.Services.AddScoped<IMovieCategoryResolver, MovieCategoryResolver>();
builder.Services.AddScoped<IMovieApiResponseParser, ConfigurableJsonParser>();
builder.Services.AddScoped<IBookResponseParser, ConfigurableBookParser>();

builder.Services.AddScoped<IGenreMapper, GenreMapper>();
builder.Services.AddScoped<IMlGenreMapper, MlMoodToGenresMapper>();

builder.Services.AddScoped<IContentDetailService, ContentDetailService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Миграции применены. База данных готова.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка применения миграций: {ex.Message}");
    }
}

app.Run();

// JSON 

public class AppSettingsRoot
{
    public MovieApiOptions MovieApi { get; set; } = new();
    public BookApiOptions BookApi { get; set; } = new();
}