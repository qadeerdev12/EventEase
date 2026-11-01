using EventEase.Models;

namespace EventEase.Services;

// In-memory mock data store. Registered as a singleton so data survives page navigation.
// Blazor Server shares this instance across all users' circuits, so writes are guarded by a lock.
public class EventService
{
    private readonly object _sync = new();
    private readonly List<Event> _events;
    private IReadOnlyList<Event>? _sortedCache;

    public EventService(int generatedEventCount = 0)
    {
        _events =
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

        _events.AddRange(GenerateEvents(generatedEventCount, firstId: _events.Count + 1));
    }

    // Sorting happens once and is cached; the cache is cleared whenever an event changes.
    public IReadOnlyList<Event> GetAll()
    {
        lock (_sync)
        {
            return _sortedCache ??= _events.OrderBy(e => e.Date).ThenBy(e => e.Id).ToList();
        }
    }

    public Event? GetById(int id)
    {
        lock (_sync)
        {
            return _events.FirstOrDefault(e => e.Id == id);
        }
    }

    public bool Update(Event updated)
    {
        lock (_sync)
        {
            var existing = _events.FirstOrDefault(e => e.Id == updated.Id);
            if (existing is null) return false;

            existing.Name = updated.Name;
            existing.Date = updated.Date;
            existing.Location = updated.Location;
            _sortedCache = null;
            return true;
        }
    }

    public bool Register(int eventId, Registration registration)
    {
        lock (_sync)
        {
            var ev = _events.FirstOrDefault(e => e.Id == eventId);
            if (ev is null || ev.SeatsLeft < registration.Guests) return false;

            ev.Registrations.Add(registration);
            return true;
        }
    }

    private static IEnumerable<Event> GenerateEvents(int count, int firstId)
    {
        string[] kinds = ["Conference", "Workshop", "Gala", "Meetup", "Retreat", "Summit", "Mixer", "Expo"];
        string[] topics = ["Cloud", "Marketing", "Finance", "Design", "Wellness", "Leadership", "AI", "Sales"];
        string[] cities = ["Seattle", "Chicago", "Austin", "Denver", "Boston", "Atlanta", "Miami", "Portland"];
        var start = new DateTime(2027, 1, 1, 9, 0, 0);
        var random = new Random(42); // fixed seed so the data is the same on every run

        for (var i = 0; i < count; i++)
        {
            yield return new Event
            {
                Id = firstId + i,
                Name = $"{topics[random.Next(topics.Length)]} {kinds[random.Next(kinds.Length)]} #{i + 1}",
                Date = start.AddHours(random.Next(0, 24 * 365 * 2)),
                Location = $"{cities[random.Next(cities.Length)]} Event Center",
                Capacity = random.Next(20, 500),
                Description = "Generated sample event for performance testing.",
            };
        }
    }
}
