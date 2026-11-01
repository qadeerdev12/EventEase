using EventEase.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventEase.Tests;

public class UserSessionTests
{
    private static UserSession NewSession(InMemorySessionStore store) =>
        new(store, NullLogger<UserSession>.Instance);

    [Fact]
    public async Task NewSession_StartsAsGuest()
    {
        var session = NewSession(new InMemorySessionStore());

        await session.EnsureLoadedAsync();

        Assert.True(session.IsLoaded);
        Assert.False(session.IsIdentified);
    }

    [Fact]
    public async Task Identity_SurvivesPageReload()
    {
        var store = new InMemorySessionStore();
        var firstPageLoad = NewSession(store);
        await firstPageLoad.IdentifyAsync("  Jordan Rivera ", " Jordan@Example.com");
        await firstPageLoad.RecordViewAsync(2);

        // A reload creates a new circuit and a new UserSession, backed by the same browser storage.
        var afterReload = NewSession(store);
        await afterReload.EnsureLoadedAsync();

        Assert.True(afterReload.IsIdentified);
        Assert.Equal("Jordan Rivera", afterReload.Name);
        Assert.Equal("jordan@example.com", afterReload.Email);
        Assert.Equal([2], afterReload.RecentEventIds);
        Assert.Equal(firstPageLoad.StartedAt, afterReload.StartedAt);
    }

    [Fact]
    public async Task RecentEvents_AreDeduplicatedNewestFirstAndCapped()
    {
        var session = NewSession(new InMemorySessionStore());

        foreach (var id in new[] { 1, 2, 3, 1, 4, 5, 6, 7 })
        {
            await session.RecordViewAsync(id);
        }

        Assert.Equal([7, 6, 5, 4, 1], session.RecentEventIds);
    }

    [Fact]
    public async Task SignOut_ClearsSavedSession()
    {
        var store = new InMemorySessionStore();
        var session = NewSession(store);
        await session.IdentifyAsync("Jordan Rivera", "jordan@example.com");

        await session.SignOutAsync();
        var afterReload = NewSession(store);
        await afterReload.EnsureLoadedAsync();

        Assert.False(session.IsIdentified);
        Assert.False(afterReload.IsIdentified);
    }

    [Fact]
    public async Task StorageFailure_DoesNotBreakTheSession()
    {
        var store = new InMemorySessionStore { ThrowOnAccess = true };
        var session = NewSession(store);

        await session.EnsureLoadedAsync();
        await session.IdentifyAsync("Jordan Rivera", "jordan@example.com");

        Assert.True(session.IsLoaded);
        Assert.Equal("Jordan Rivera", session.Name); // still works in memory for this tab
    }

    [Fact]
    public async Task Changed_IsRaisedWhenStateChanges()
    {
        var session = NewSession(new InMemorySessionStore());
        var count = 0;
        session.Changed += () => count++;

        await session.EnsureLoadedAsync();
        await session.IdentifyAsync("Jordan Rivera", "jordan@example.com");

        Assert.Equal(2, count);
    }
}
