using LibraryManagementSystem.Api.Data;
using LibraryManagementSystem.Api.DTOs;
using LibraryManagementSystem.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoansController : ControllerBase
{
    private readonly LibraryContext _context;

    public LoansController(LibraryContext context)
    {
        _context = context;
    }

    // GET: api/loans
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoanReadDto>>> GetLoans([FromQuery] bool? active)
    {
        var query = _context.Loans.AsQueryable();

        if (active is true)
        {
            query = query.Where(l => l.ReturnDate == null);
        }
        else if (active is false)
        {
            query = query.Where(l => l.ReturnDate != null);
        }

        // Projected inline (rather than via ToReadDto) so EF Core can translate
        // this straight into a SQL join instead of loading full entities first.
        var loans = await query
            .OrderByDescending(l => l.BorrowDate)
            .Select(l => new LoanReadDto
            {
                Id = l.Id,
                BookId = l.BookId,
                BookTitle = l.Book!.Title,
                MemberId = l.MemberId,
                MemberName = l.Member!.FullName,
                BorrowDate = l.BorrowDate,
                DueDate = l.DueDate,
                ReturnDate = l.ReturnDate,
                IsReturned = l.ReturnDate != null
            })
            .ToListAsync();

        return Ok(loans);
    }

    // GET: api/loans/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LoanReadDto>> GetLoan(int id)
    {
        var loan = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan is null)
        {
            return NotFound(new { message = $"Loan with id {id} was not found." });
        }

        return Ok(ToReadDto(loan));
    }

    // POST: api/loans  (borrow a book)
    [HttpPost]
    public async Task<ActionResult<LoanReadDto>> CreateLoan(LoanCreateDto dto)
    {
        var book = await _context.Books.FindAsync(dto.BookId);
        if (book is null)
        {
            return NotFound(new { message = $"Book with id {dto.BookId} was not found." });
        }

        var member = await _context.Members.FindAsync(dto.MemberId);
        if (member is null)
        {
            return NotFound(new { message = $"Member with id {dto.MemberId} was not found." });
        }

        if (book.AvailableCopies <= 0)
        {
            return BadRequest(new { message = $"No available copies of '{book.Title}' to lend." });
        }

        var loan = new Loan
        {
            BookId = book.Id,
            MemberId = member.Id,
            BorrowDate = DateTime.UtcNow,
            DueDate = dto.DueDate ?? DateTime.UtcNow.AddDays(14)
        };

        book.AvailableCopies -= 1;

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        loan.Book = book;
        loan.Member = member;

        return CreatedAtAction(nameof(GetLoan), new { id = loan.Id }, ToReadDto(loan));
    }

    // PUT: api/loans/5/return  (return a book)
    [HttpPut("{id:int}/return")]
    public async Task<ActionResult<LoanReadDto>> ReturnLoan(int id)
    {
        var loan = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Member)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan is null)
        {
            return NotFound(new { message = $"Loan with id {id} was not found." });
        }

        if (loan.ReturnDate is not null)
        {
            return BadRequest(new { message = "This loan has already been returned." });
        }

        loan.ReturnDate = DateTime.UtcNow;
        if (loan.Book is not null)
        {
            loan.Book.AvailableCopies = Math.Min(loan.Book.TotalCopies, loan.Book.AvailableCopies + 1);
        }

        await _context.SaveChangesAsync();

        return Ok(ToReadDto(loan));
    }

    // DELETE: api/loans/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteLoan(int id)
    {
        var loan = await _context.Loans.Include(l => l.Book).FirstOrDefaultAsync(l => l.Id == id);
        if (loan is null)
        {
            return NotFound(new { message = $"Loan with id {id} was not found." });
        }

        // If the loan was still active, release the copy back to the pool before deleting the record
        if (loan.ReturnDate is null && loan.Book is not null)
        {
            loan.Book.AvailableCopies = Math.Min(loan.Book.TotalCopies, loan.Book.AvailableCopies + 1);
        }

        _context.Loans.Remove(loan);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static LoanReadDto ToReadDto(Loan l) => new()
    {
        Id = l.Id,
        BookId = l.BookId,
        BookTitle = l.Book?.Title ?? string.Empty,
        MemberId = l.MemberId,
        MemberName = l.Member?.FullName ?? string.Empty,
        BorrowDate = l.BorrowDate,
        DueDate = l.DueDate,
        ReturnDate = l.ReturnDate,
        IsReturned = l.IsReturned
    };
}
