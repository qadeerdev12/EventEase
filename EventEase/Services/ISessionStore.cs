using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace EventEase.Services;

// Where the user session is saved between page reloads.
public interface ISessionStore
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task DeleteAsync(string key);
}

// Saves to the browser's sessionStorage, encrypted with ASP.NET Core Data Protection so it
// can't be read or tampered with on the client. Data lasts until the browser tab is closed.
public class ProtectedSessionStore(ProtectedSessionStorage storage) : ISessionStore
{
    public async Task<T?> GetAsync<T>(string key)
    {
        var result = await storage.GetAsync<T>(key);
        return result.Success ? result.Value : default;
    }

    public async Task SetAsync<T>(string key, T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        await storage.SetAsync(key, value);
    }

    public async Task DeleteAsync(string key) => await storage.DeleteAsync(key);
}
