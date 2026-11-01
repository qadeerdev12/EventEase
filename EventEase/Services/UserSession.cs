namespace EventEase.Services;

// Tracks the current visitor for one browser tab: who they are and which events they've
// looked at. Registered as scoped, so each Blazor circuit (tab) gets its own instance.
// State is saved to ISessionStore so it survives page reloads.
public class UserSession(ISessionStore store, ILogger<UserSession> logger)
{
    public const string StorageKey = "eventease.session";
    public const int MaxRecentEvents = 5;

    private SessionData _data = new();
    private Task? _loadTask;

    public event Action? Changed;

    public bool IsLoaded { get; private set; }
    public string? Name => _data.Name;
    public string? Email => _data.Email;
    public bool IsIdentified => !string.IsNullOrEmpty(_data.Email);
    public DateTime StartedAt => _data.StartedAt;
    public IReadOnlyList<int> RecentEventIds => _data.RecentEventIds;

    // Browser storage can only be read once the page is interactive (not during prerendering),
    // so components call this from OnAfterRenderAsync. Safe to call many times.
    public Task EnsureLoadedAsync() => _loadTask ??= LoadAsync();

    public async Task IdentifyAsync(string name, string email)
    {
        await EnsureLoadedAsync(); // don't let a slow restore overwrite this change
        _data.Name = name.Trim();
        _data.Email = email.Trim().ToLowerInvariant();
        await SaveAsync();
    }

    public async Task RecordViewAsync(int eventId)
    {
        await EnsureLoadedAsync();
        _data.RecentEventIds.Remove(eventId);
        _data.RecentEventIds.Insert(0, eventId);
        if (_data.RecentEventIds.Count > MaxRecentEvents)
        {
            _data.RecentEventIds.RemoveRange(MaxRecentEvents, _data.RecentEventIds.Count - MaxRecentEvents);
        }
        await SaveAsync();
    }

    public async Task SignOutAsync()
    {
        _data = new SessionData();
        try
        {
            await store.DeleteAsync(StorageKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not clear the saved session.");
        }
        Changed?.Invoke();
    }

    // Storage calls can fail if the circuit disconnects or saved data can't be decrypted,
    // so failures are logged and the session carries on in memory.
    private async Task LoadAsync()
    {
        try
        {
            if (await store.GetAsync<SessionData>(StorageKey) is { } saved)
            {
                _data = saved;
            }
        }
        catch (Exception ex)
        {
            // Unreadable or tampered data: start a fresh session rather than failing the page.
            logger.LogWarning(ex, "Could not restore the saved session; starting a new one.");
        }

        IsLoaded = true;
        Changed?.Invoke();
    }

    private async Task SaveAsync()
    {
        try
        {
            await store.SetAsync(StorageKey, _data);
        }
        catch (Exception ex)
        {
            // The session still works for this tab; it just won't survive a reload.
            logger.LogWarning(ex, "Could not save the session.");
        }
        Changed?.Invoke();
    }

    public class SessionData
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public List<int> RecentEventIds { get; set; } = [];
    }
}
