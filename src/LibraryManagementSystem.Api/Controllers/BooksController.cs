using LibraryManagementSystem.Api.Data;
using LibraryManagementSystem.Api.DTOs;
using LibraryManagementSystem.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly LibraryContext _context;

    public BooksController(LibraryContext context)
    {
        _context = context;
    }

    // GET: api/books
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookReadDto>>> GetBooks()
    {
        // The projection is written inline (rather than calling ToReadDto here)
        // so EF Core can translate it directly into SQL instead of pulling
        // every column back and mapping in memory.
        var books = await _context.Books
            .OrderBy(b => b.Title)
            .Select(b => new BookReadDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Isbn = b.Isbn,
                Genre = b.Genre,
                PublishedYear = b.PublishedYear,
                TotalCopies = b.TotalCopies,
                AvailableCopies = b.AvailableCopies
            })
            .ToListAsync();

        return Ok(books);
    }

    // GET: api/books/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookReadDto>> GetBook(int id)
    {
        var book = await _context.Books.FindAsync(id);

        if (book is null)
        {
            return NotFound(new { message = $"Book with id {id} was not found." });
        }

        return Ok(ToReadDto(book));
    }

    // POST: api/books
    [HttpPost]
    public async Task<ActionResult<BookReadDto>> CreateBook(BookWriteDto dto)
    {
        if (await _context.Books.AnyAsync(b => b.Isbn == dto.Isbn))
        {
            return Conflict(new { message = $"A book with ISBN {dto.Isbn} already exists." });
        }

        var book = new Book
        {
            Title = dto.Title,
            Author = dto.Author,
            Isbn = dto.Isbn,
            Genre = dto.Genre,
            PublishedYear = dto.PublishedYear,
            TotalCopies = dto.TotalCopies,
            AvailableCopies = dto.TotalCopies
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBook), new { id = book.Id }, ToReadDto(book));
    }

    // PUT: api/books/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBook(int id, BookWriteDto dto)
    {
        var book = await _context.Books.FindAsync(id);
        if (book is null)
        {
            return NotFound(new { message = $"Book with id {id} was not found." });
        }

        if (await _context.Books.AnyAsync(b => b.Isbn == dto.Isbn && b.Id != id))
        {
            return Conflict(new { message = $"Another book already uses ISBN {dto.Isbn}." });
        }

        // Keep the number of currently-loaned copies consistent when total changes
        var copiesOnLoan = book.TotalCopies - book.AvailableCopies;

        book.Title = dto.Title;
        book.Author = dto.Author;
        book.Isbn = dto.Isbn;
        book.Genre = dto.Genre;
        book.PublishedYear = dto.PublishedYear;
        book.TotalCopies = dto.TotalCopies;
        book.AvailableCopies = Math.Max(0, dto.TotalCopies - copiesOnLoan);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/books/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book is null)
        {
            return NotFound(new { message = $"Book with id {id} was not found." });
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static BookReadDto ToReadDto(Book b) => new()
    {
        Id = b.Id,
        Title = b.Title,
        Author = b.Author,
        Isbn = b.Isbn,
        Genre = b.Genre,
        PublishedYear = b.PublishedYear,
        TotalCopies = b.TotalCopies,
        AvailableCopies = b.AvailableCopies
    };
}
