using EventEase.Models;

namespace EventEase.Services;

// In-memory mock data store. Registered as a singleton so data survives page navigation.
// Blazor Server shares this instance across all users' circuits, so writes are guarded by a lock.
public class EventService
{
    private readonly object _sync = new();
    private readonly List<Event> _events;
    private readonly TimeProvider _time;
    private IReadOnlyList<Event>? _sortedCache;
    private int _nextRegistrationId = 1;

    // Raised after any registration or check-in so open pages (e.g. the attendance tracker) can refresh.
    public event Action? Changed;

    public EventService(int generatedEventCount = 0, TimeProvider? time = null)
    {
        _time = time ?? TimeProvider.System;
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

        SeedSampleRegistrations();
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

    public RegistrationResult Register(int eventId, Registration form)
    {
        Registration saved;
        lock (_sync)
        {
            var ev = _events.FirstOrDefault(e => e.Id == eventId);
            if (ev is null) return new(RegistrationStatus.EventNotFound);
            if (ev.Date < Now) return new(RegistrationStatus.EventEnded);

            var email = form.Email.Trim();
            if (ev.Registrations.Any(r => string.Equals(r.Email, email, StringComparison.OrdinalIgnoreCase)))
            {
                return new(RegistrationStatus.AlreadyRegistered);
            }
            if (form.Seats < 1 || ev.SeatsLeft < form.Seats) return new(RegistrationStatus.NotEnoughSeats);

            // Store a copy so later edits to the form object can't change saved data.
            saved = new Registration
            {
                Id = _nextRegistrationId++,
                EventId = eventId,
                RegisteredAt = Now,
                FullName = form.FullName.Trim(),
                Email = email.ToLowerInvariant(),
                Phone = string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim(),
                Seats = form.Seats,
                AcceptTerms = form.AcceptTerms,
            };
            ev.Registrations.Add(saved);
        }

        Changed?.Invoke();
        return new(RegistrationStatus.Success, saved);
    }

    public bool SetCheckedIn(int eventId, int registrationId, bool checkedIn)
    {
        lock (_sync)
        {
            var registration = _events.FirstOrDefault(e => e.Id == eventId)?
                .Registrations.FirstOrDefault(r => r.Id == registrationId);
            if (registration is null) return false;

            registration.CheckedInAt = checkedIn ? registration.CheckedInAt ?? Now : null;
        }

        Changed?.Invoke();
        return true;
    }

    // Returns a snapshot so callers can enumerate safely while other users register.
    public IReadOnlyList<Registration> GetRegistrations(int eventId)
    {
        lock (_sync)
        {
            return _events.FirstOrDefault(e => e.Id == eventId)?.Registrations
                .OrderBy(r => r.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
        }
    }

    public IReadOnlyList<UserRegistration> GetRegistrationsForEmail(string email)
    {
        var normalized = email.Trim();
        lock (_sync)
        {
            return _events
                .SelectMany(e => e.Registrations
                    .Where(r => string.Equals(r.Email, normalized, StringComparison.OrdinalIgnoreCase))
                    .Select(r => new UserRegistration(e, r)))
                .OrderBy(x => x.Event.Date)
                .ToList();
        }
    }

    public IReadOnlyList<AttendanceSummary> GetAttendance()
    {
        var events = GetAll();
        lock (_sync)
        {
            return events
                .Select(e => new AttendanceSummary(
                    e,
                    e.Registrations.Count,
                    e.Registrations.Sum(r => r.Seats),
                    e.Registrations.Where(r => r.IsCheckedIn).Sum(r => r.Seats)))
                .ToList();
        }
    }

    private DateTime Now => _time.GetLocalNow().DateTime;

    // A few demo attendees so the attendance tracker has data on first run.
    private void SeedSampleRegistrations()
    {
        (int EventId, string Name, int Seats, bool CheckedIn)[] samples =
        [
            (1, "Avery Johnson", 2, true),
            (1, "Priya Patel", 1, true),
            (1, "Marcus Lee", 3, false),
            (2, "Sofia Garcia", 2, false),
            (3, "Daniel Kim", 1, false),
        ];

        foreach (var (eventId, name, seats, checkedIn) in samples)
        {
            var seededAt = Now;
            _events.First(e => e.Id == eventId).Registrations.Add(new Registration
            {
                Id = _nextRegistrationId++,
                EventId = eventId,
                FullName = name,
                Email = $"{name.Split(' ')[0].ToLowerInvariant()}@example.com",
                Seats = seats,
                AcceptTerms = true,
                RegisteredAt = seededAt,
                CheckedInAt = checkedIn ? seededAt : null,
            });
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
