using System.ComponentModel.DataAnnotations;

namespace EventEase.Models;

public class Registration
{
    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(80)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Range(1, 10, ErrorMessage = "You can register 1 to 10 guests.")]
    public int Guests { get; set; } = 1;
}
