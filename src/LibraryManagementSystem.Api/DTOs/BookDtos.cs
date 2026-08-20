using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Api.DTOs;

public class BookReadDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int PublishedYear { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}

public class BookWriteDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Author { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Isbn { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Genre { get; set; } = string.Empty;

    [Range(0, 3000)]
    public int PublishedYear { get; set; }

    [Range(0, 10000)]
    public int TotalCopies { get; set; }
}
