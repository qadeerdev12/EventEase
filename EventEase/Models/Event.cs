using System.ComponentModel.DataAnnotations;

namespace EventEase.Models;

public class Event
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required, StringLength(100)]
    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public List<Registration> Registrations { get; } = new();

    public int SeatsLeft => Capacity - Registrations.Sum(r => r.Guests);
}
