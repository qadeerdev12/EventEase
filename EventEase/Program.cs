using EventEase.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Set EventEase:GeneratedEventCount (e.g. 5000) to load extra sample events for performance testing.
builder.Services.AddSingleton(_ => new EventEase.Services.EventService(
    builder.Configuration.GetValue("EventEase:GeneratedEventCount", 0)));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Exposes Program to the integration tests in EventEase.Tests.
public partial class Program;
