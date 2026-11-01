using EventEase.Models;
using EventEase.Services;

namespace EventEase.Tests;

// Stands in for browser sessionStorage. Sharing one instance between two UserSession
// objects simulates the same tab after a page reload.
public class InMemorySessionStore : ISessionStore
{
    private readonly Dictionary<string, object?> _values = new();

    public bool ThrowOnAccess { get; set; }

    public Task<T?> GetAsync<T>(string key)
    {
        ThrowIfBroken();
        return Task.FromResult(_values.TryGetValue(key, out var value) ? (T?)value : default);
    }

    public Task SetAsync<T>(string key, T value)
    {
        ThrowIfBroken();
        // Store a JSON copy, like real storage, so later changes to the object aren't shared.
        _values[key] = System.Text.Json.JsonSerializer.Deserialize<T>(System.Text.Json.JsonSerializer.Serialize(value));
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key)
    {
        ThrowIfBroken();
        _values.Remove(key);
        return Task.CompletedTask;
    }

    private void ThrowIfBroken()
    {
        if (ThrowOnAccess) throw new InvalidOperationException("Storage unavailable");
    }
}

// A clock fixed at a chosen time, so date rules can be tested.
public class FixedTimeProvider(DateTime now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(now, DateTimeKind.Utc));
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

public static class TestData
{
    public static Registration ValidForm(string email = "jordan@example.com", int seats = 1) => new()
    {
        FullName = "Jordan Rivera",
        Email = email,
        Phone = "555-123-4567",
        Seats = seats,
        AcceptTerms = true,
    };
}
