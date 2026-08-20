namespace LibraryManagementSystem.Api.Models;

public class Loan
{
    public int Id { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }

    public int MemberId { get; set; }
    public Member? Member { get; set; }

    public DateTime BorrowDate { get; set; } = DateTime.UtcNow;

    public DateTime DueDate { get; set; }

    // Null while the book is still out on loan
    public DateTime? ReturnDate { get; set; }

    public bool IsReturned => ReturnDate.HasValue;
}
