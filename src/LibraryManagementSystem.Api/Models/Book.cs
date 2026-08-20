using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Api.Models;

public class Book
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Author { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Isbn { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Genre { get; set; } = string.Empty;

    public int PublishedYear { get; set; }

    public int TotalCopies { get; set; }

    public int AvailableCopies { get; set; }

    public List<Loan> Loans { get; set; } = new();
}
