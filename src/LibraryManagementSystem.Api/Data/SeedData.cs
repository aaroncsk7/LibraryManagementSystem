using LibraryManagementSystem.Api.Models;

namespace LibraryManagementSystem.Api.Data;

public static class SeedData
{
    public static void Initialize(LibraryContext context)
    {
        // Only seed an empty database
        if (context.Books.Any() || context.Members.Any())
        {
            return;
        }

        var books = new List<Book>
        {
            new()
            {
                Title = "Clean Code",
                Author = "Robert C. Martin",
                Isbn = "9780132350884",
                Genre = "Software Engineering",
                PublishedYear = 2008,
                TotalCopies = 3,
                AvailableCopies = 3
            },
            new()
            {
                Title = "The Pragmatic Programmer",
                Author = "David Thomas & Andrew Hunt",
                Isbn = "9780135957059",
                Genre = "Software Engineering",
                PublishedYear = 2019,
                TotalCopies = 2,
                AvailableCopies = 2
            },
            new()
            {
                Title = "Dune",
                Author = "Frank Herbert",
                Isbn = "9780441013593",
                Genre = "Science Fiction",
                PublishedYear = 1965,
                TotalCopies = 4,
                AvailableCopies = 4
            }
        };

        var members = new List<Member>
        {
            new() { FullName = "Ada Lovelace", Email = "ada@example.com", PhoneNumber = "555-0101" },
            new() { FullName = "Alan Turing", Email = "alan@example.com", PhoneNumber = "555-0102" }
        };

        context.Books.AddRange(books);
        context.Members.AddRange(members);
        context.SaveChanges();
    }
}
