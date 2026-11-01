using System.ComponentModel.DataAnnotations;

namespace EventEase.Models;

public class Registration
{
    public const int MaxSeats = 10;
    public const string NamePattern = @"^\s*[\p{L}][\p{L} .'\-]*\s*$";
    public const string NameError = "Name can only contain letters, spaces, apostrophes, periods and hyphens.";
    public const string EmailPattern = @"^\s*[^@\s]+@[^@\s]+\.[^@\s]{2,}\s*$";
    public const string EmailError = "Please enter a valid email address, like name@example.com.";

    // Set by EventService when the registration is saved.
    public int Id { get; set; }
    public int EventId { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public bool IsCheckedIn => CheckedInAt is not null;

    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Name must be 2 to 80 characters.")]
    [RegularExpression(NamePattern, ErrorMessage = NameError)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [StringLength(254)]
    [RegularExpression(EmailPattern, ErrorMessage = EmailError)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(20, ErrorMessage = "Phone number is too long.")]
    public string? Phone
    {
        get => _phone;
        // A cleared field arrives as "" which [Phone] rejects; treat blank as "not provided".
        set => _phone = string.IsNullOrWhiteSpace(value) ? null : value;
    }
    private string? _phone;

    [Range(1, MaxSeats, ErrorMessage = "You can reserve 1 to 10 seats.")]
    public int Seats { get; set; } = 1;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Please accept the event terms to continue.")]
    public bool AcceptTerms { get; set; }

    public void Normalize()
    {
        FullName = FullName.Trim();
        Email = Email.Trim().ToLowerInvariant();
        Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
    }
}
