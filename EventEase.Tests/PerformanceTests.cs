using System.Diagnostics;
using System.Text.RegularExpressions;
using Bunit;
using EventEase.Components.Pages;
using EventEase.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace EventEase.Tests;

public class PerformanceTests(ITestOutputHelper output) : BunitContext
{
    private const int LargeEventCount = 5000;

    [Fact]
    public void EventList_WithLargeDataset_RendersOnlyVisibleCards()
    {
        Services.AddSingleton(new EventService(LargeEventCount));
        JSInterop.Mode = JSRuntimeMode.Loose; // Virtualize calls into JS to observe scrolling

        var timer = Stopwatch.StartNew();
        var cut = Render<EventList>();
        timer.Stop();

        var cards = cut.FindAll(".event-card").Count;
        output.WriteLine($"{LargeEventCount + 4} events: {cards} cards rendered in {timer.ElapsedMilliseconds} ms");

        Assert.Contains($"Showing {LargeEventCount + 4} of {LargeEventCount + 4} events", cut.Markup);
        // Without a browser there is no viewport to measure, so Virtualize renders its maximum:
        // 30 rows plus the overscan buffer, 3 cards per row. A browser renders only what is on screen.
        Assert.InRange(cards, 1, (30 + 2 * 4) * 3);
    }

    [Fact]
    public void Search_FiltersEvents()
    {
        Services.AddSingleton(new EventService(LargeEventCount));
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<EventList>();

        cut.Find("#event-search").Input("Charity Gala");

        Assert.Contains("Showing 1 of", cut.Markup);
        Assert.Contains("Annual Charity Gala", cut.Markup);
    }

    [Fact]
    public void Search_WithNoMatches_ShowsMessage()
    {
        Services.AddSingleton(new EventService());
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<EventList>();

        cut.Find("#event-search").Input("zzz-no-match");

        Assert.Contains("No events match your search.", cut.Markup);
    }

    [Fact]
    public void GetAll_ReturnsCachedSortedListUntilAnEventChanges()
    {
        var service = new EventService(LargeEventCount);

        var first = service.GetAll();
        Assert.Same(first, service.GetAll());
        Assert.True(first.Zip(first.Skip(1)).All(pair => pair.First.Date <= pair.Second.Date));

        var ev = service.GetById(1)!;
        service.Update(new() { Id = 1, Name = "Renamed", Date = ev.Date, Location = ev.Location });
        Assert.NotSame(first, service.GetAll());
    }

    [Fact]
    public async Task EventListPage_WithLargeDataset_ServesSmallPage()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("EventEase:GeneratedEventCount", LargeEventCount.ToString()));
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/events");
        var cards = Regex.Matches(html, "class=\"card event-card").Count;
        output.WriteLine($"/events HTML: {html.Length / 1024} KB, {cards} cards");

        // Before virtualization this page was ~2.7 MB with all 5,004 cards. Virtualize fills in
        // the visible cards once the page is interactive, so the first HTML response stays small.
        Assert.Contains($"Showing {LargeEventCount + 4} of {LargeEventCount + 4} events", html);
        Assert.InRange(cards, 0, 100);
        Assert.True(html.Length < 100_000, $"Page was {html.Length} bytes");
    }
}
