using EventEase.Components;
using EventEase.Services;

var builder = WebApplication.CreateBuilder(args);

// Set EventEase:GeneratedEventCount (e.g. 5000) to load extra sample events for performance testing.
builder.Services.AddSingleton(_ => new EventService(
    builder.Configuration.GetValue("EventEase:GeneratedEventCount", 0)));

// One session per browser tab (Blazor circuit), saved to encrypted browser sessionStorage.
builder.Services.AddScoped<ISessionStore, ProtectedSessionStore>();
builder.Services.AddScoped<UserSession>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/healthz"); // for load balancers and hosting platforms
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Exposes Program to the integration tests in EventEase.Tests.
public partial class Program;
