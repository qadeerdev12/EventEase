using EventEase.Models;

namespace EventEase.Services;

// In-memory mock data store. Registered as a singleton so data survives page navigation.
public class EventService
{
    private readonly List<Event> _events =
    [
        new() { Id = 1, Name = "Tech Innovators Summit", Date = new DateTime(2026, 12, 3, 9, 0, 0),
                Location = "Seattle Convention Center", Capacity = 250,
                Description = "A full-day summit on AI, cloud, and the future of work." },
        new() { Id = 2, Name = "Annual Charity Gala", Date = new DateTime(2026, 12, 12, 19, 0, 0),
                Location = "The Grand Ballroom, Chicago", Capacity = 150,
                Description = "An evening of dinner, live music, and fundraising for local schools." },
        new() { Id = 3, Name = "Leadership Offsite", Date = new DateTime(2027, 1, 15, 8, 30, 0),
                Location = "Lakeside Retreat, Lake Tahoe", Capacity = 40,
                Description = "Two days of strategy workshops and team-building for senior leaders." },
        new() { Id = 4, Name = "Spring Networking Mixer", Date = new DateTime(2027, 3, 20, 18, 0, 0),
                Location = "Rooftop Lounge, Austin", Capacity = 120,
                Description = "Meet professionals across industries over drinks and appetizers." },
    ];

    public IReadOnlyList<Event> GetAll() => _events.OrderBy(e => e.Date).ToList();

    public Event? GetById(int id) => _events.FirstOrDefault(e => e.Id == id);

    public bool Register(int eventId, Registration registration)
    {
        var ev = GetById(eventId);
        if (ev is null || ev.SeatsLeft < registration.Guests) return false;

        ev.Registrations.Add(registration);
        return true;
    }
}
