using ContentRecommender.Core.Configuration;
using ContentRecommender.Core.Models;
using ContentRecommender.Core.Services;
using ContentRecommender.Data;
using ContentRecommender.Data.Repositories;
using ContentRecommender.Web.Components;
using ContentRecommender.Web.ML.Services;
using ContentRecommender.Web.Services;
using ContentRecommender.Web.Services.MovieSearch;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();

// Добавляем поддержку Razor Pages (для аутентификации)
builder.Services.AddRazorPages()
    .AddRazorRuntimeCompilation();

// База данных
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
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

// Настройка cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// Добавляем аутентификацию и авторизацию
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Загружаем конфигурации
var googleBooksConfig = builder.Configuration.GetSection("GoogleBooks").Get<GoogleBooksConfig>()
    ?? throw new InvalidOperationException("GoogleBooks configuration is missing");
var cacheSettings = builder.Configuration.GetSection("CacheSettings").Get<CacheSettings>()
    ?? new CacheSettings { DurationDays = 7, MaxItemsPerCategory = 15 };

// Оставляем синглтоны для Google Books и кэша (пока)
builder.Services.AddSingleton(googleBooksConfig);
builder.Services.AddSingleton(cacheSettings);

// Репозитории
builder.Services.AddScoped<IFavoritesRepository, FavoritesRepository>();

// HTTP клиент для Google Books (старый, потом заменим)
builder.Services.AddHttpClient<GoogleBooksService>((sp, client) =>
{
    var config = sp.GetRequiredService<GoogleBooksConfig>();
    client.BaseAddress = new Uri(config.BaseUrl ?? "https://www.googleapis.com/books/v1/volumes");
});

// ML Context
builder.Services.AddSingleton<MLContext>();
builder.Services.AddScoped<IMoodAnalysisService, MoodAnalysisService>();

// Другие сервисы
builder.Services.AddScoped<ContentSearchService>();
builder.Services.AddScoped<IFavoritesService, FavoritesService>();
builder.Services.AddScoped<ISearchStateService, SearchStateService>();

// Кэш
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IContentCacheService, ContentCacheService>();


// Конфигурация API (MovieApi секция в appsettings.json)
builder.Services.Configure<ApiAdapterConfig>(builder.Configuration.GetSection("MovieApi"));

// ML маппер настроений в жанры
builder.Services.AddScoped<IMlGenreMapper, MlMoodToGenresMapper>();

// Компоненты универсального поиска
builder.Services.AddScoped<IGenreMapper, GenreMapper>();
builder.Services.AddScoped<IMovieApiUrlBuilder, MovieApiUrlBuilder>();
builder.Services.AddScoped<IMovieCategoryResolver, MovieCategoryResolver>();
builder.Services.AddScoped<IMovieApiResponseParser, ConfigurableJsonParser>();

builder.Services.AddHttpClient<IMovieDetailService, GenericMovieDetailService>();

// ContentDetailService
builder.Services.AddScoped<IContentDetailService, ContentDetailService>();
// Основной сервис поиска фильмов
builder.Services.AddHttpClient<IMovieSearchService, GenericMovieSearchService>();

var app = builder.Build();

// Настройка конвейера HTTP-запросов
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

// Маршруты
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers();

// Применяем миграции
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