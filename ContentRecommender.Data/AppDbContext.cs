using ContentRecommender.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace ContentRecommender.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ContentItem> ContentItems { get; set; } = null!;
    public DbSet<Movie> Movies { get; set; } = null!;
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<ContentCache> ContentCache { get; set; } = null!;

    public DbSet<UserPreferences> UserPreferences { get; set; } = null!;
    public DbSet<SearchHistory> SearchHistory { get; set; } = null!;
    public DbSet<FavoriteItem> Favorites { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ContentItem>()
            .HasDiscriminator<ContentFormat>("ContentFormat")
            .HasValue<Movie>(ContentFormat.Movie)
            .HasValue<Book>(ContentFormat.Book);

        modelBuilder.Entity<UserPreferences>()
            .HasOne(p => p.User)
            .WithOne(u => u.Preferences)
            .HasForeignKey<UserPreferences>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FavoriteItem>()
            .HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SearchHistory>()
            .HasOne(h => h.User)
            .WithMany(u => u.SearchHistory)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var jsonOptions = new JsonSerializerOptions();

        static ValueComparer<List<T>> CreateListComparer<T>() where T : notnull
        {
            return new ValueComparer<List<T>>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());
        }

        modelBuilder.Entity<ContentItem>()
            .Property(c => c.Genres)
            .HasConversion(
                v => v != null ? JsonSerializer.Serialize(v, jsonOptions) : null,
                v => !string.IsNullOrEmpty(v) ? JsonSerializer.Deserialize<List<string>>(v, jsonOptions) : null)
            .Metadata.SetValueComparer(CreateListComparer<string>());

        modelBuilder.Entity<ContentItem>()
            .Property(c => c.MoodTags)
            .HasConversion(
                v => v != null ? JsonSerializer.Serialize(v, jsonOptions) : null,
                v => !string.IsNullOrEmpty(v) ? JsonSerializer.Deserialize<List<string>>(v, jsonOptions) : null)
            .Metadata.SetValueComparer(CreateListComparer<string>());

        modelBuilder.Entity<Movie>()
            .Property(m => m.Actors)
            .HasConversion(
                v => v != null ? JsonSerializer.Serialize(v, jsonOptions) : null,
                v => !string.IsNullOrEmpty(v) ? JsonSerializer.Deserialize<List<string>>(v, jsonOptions) : null)
            .Metadata.SetValueComparer(CreateListComparer<string>());

        modelBuilder.Entity<UserPreferences>()
            .Property(u => u.PreferredMoods)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>())
            .Metadata.SetValueComparer(CreateListComparer<string>());

        modelBuilder.Entity<UserPreferences>()
            .Property(u => u.PreferredContentTypes)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<ContentTypeCategory>>(v, jsonOptions) ?? new List<ContentTypeCategory>())
            .Metadata.SetValueComparer(CreateListComparer<ContentTypeCategory>());

        modelBuilder.Entity<UserPreferences>()
            .Property(u => u.PreferredBookCategories)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<BookCategory>>(v, jsonOptions) ?? new List<BookCategory>())
            .Metadata.SetValueComparer(CreateListComparer<BookCategory>());

        modelBuilder.Entity<ContentItem>().HasIndex(c => c.ExternalId);

        modelBuilder.Entity<ContentItem>().HasIndex(c => c.Format);

        modelBuilder.Entity<ContentCache>().HasKey(c => new { c.ExternalId, c.Source });

        modelBuilder.Entity<ContentCache>().HasIndex(c => c.CachedAt);


        modelBuilder.Entity<UserPreferences>()
            .HasIndex(u => u.UserId)
            .IsUnique();

        modelBuilder.Entity<SearchHistory>().HasIndex(s => s.UserId);

        modelBuilder.Entity<FavoriteItem>()
            .HasIndex(f => new { f.UserId, f.ExternalId, f.Source })
            .IsUnique();
    }
}