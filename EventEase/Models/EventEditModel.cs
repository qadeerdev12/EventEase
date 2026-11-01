using System.ComponentModel.DataAnnotations;

namespace EventEase.Models;

// Editable copy of an event. The card edits this draft and only copies it back to the
// real event once it passes validation, so invalid input never reaches shared data.
public class EventEditModel : IValidatableObject
{
    public const int MaxYearsAhead = 5;

    [Required(ErrorMessage = "Event name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Event name must be 3 to 100 characters.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Date is required.")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Location must be 2 to 100 characters.")]
    public string? Location { get; set; }

    public static EventEditModel From(Event ev) => new()
    {
        Name = ev.Name,
        Date = ev.Date,
        Location = ev.Location,
    };

    public void ApplyTo(Event ev)
    {
        ev.Name = Name!.Trim();
        ev.Date = Date!.Value;
        ev.Location = Location!.Trim();
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Date is { } date)
        {
            if (date < DateTime.Now)
            {
                yield return new ValidationResult("Date must be in the future.", [nameof(Date)]);
            }
            else if (date > DateTime.Now.AddYears(MaxYearsAhead))
            {
                yield return new ValidationResult($"Date must be within {MaxYearsAhead} years.", [nameof(Date)]);
            }
        }

        // StringLength counts surrounding spaces, so "  a  " would pass; check the trimmed values too.
        if (Name is not null && Name.Trim().Length < 3)
        {
            yield return new ValidationResult("Event name must be 3 to 100 characters.", [nameof(Name)]);
        }
        if (Location is not null && Location.Trim().Length < 2)
        {
            yield return new ValidationResult("Location must be 2 to 100 characters.", [nameof(Location)]);
        }
    }
}
