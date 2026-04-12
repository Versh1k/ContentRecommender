namespace ContentRecommender.Core.Models;

public enum BookCategory
{
    Fiction,
    NonFiction,
    Children,
    Comics,
    Educational
}

public class Book : ContentItem
{
    public string? Author { get; set; }
    public int? Pages { get; set; }
    public BookCategory BookCategory { get; set; } = BookCategory.Fiction;

    public Book()
    {
        Format = ContentFormat.Book;
    }

}