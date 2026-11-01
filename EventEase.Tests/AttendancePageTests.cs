using Bunit;
using EventEase.Components.Pages;
using EventEase.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventEase.Tests;

public class AttendancePageTests : BunitContext
{
    private readonly EventService _events = new();

    public AttendancePageTests()
    {
        Services.AddSingleton(_events);
        JSInterop.Mode = JSRuntimeMode.Loose; // Virtualize uses JS to track scrolling
    }

    [Fact]
    public void Tracker_ShowsTotalsFromSeededData()
    {
        var cut = Render<Attendance>();

        // Seeded: 5 registrations, 9 seats booked, 3 seats checked in.
        var totals = cut.FindAll(".fs-3").Select(e => e.TextContent).ToList();
        Assert.Equal("5", totals[0]);
        Assert.Equal("9", totals[1]);
        Assert.Equal("3", totals[2]);
        Assert.Equal("33%", totals[3]);
    }

    [Fact]
    public void Tracker_UpdatesLiveWhenSomeoneRegisters()
    {
        var cut = Render<Attendance>();

        _events.Register(4, TestData.ValidForm(seats: 1));

        cut.WaitForAssertion(() => Assert.Contains("Spring Networking Mixer", cut.Markup));
        Assert.Equal("6", cut.FindAll(".fs-3")[0].TextContent);
    }

    [Fact]
    public void EventAttendance_CheckInButtonMarksAttendee()
    {
        var cut = Render<EventAttendance>(p => p.Add(c => c.Id, 1));
        Assert.Contains("3</strong> of <strong>6</strong> booked seats checked in", cut.Markup);

        cut.Find("button[aria-label='Check in Marcus Lee']").Click();

        Assert.Contains("6</strong> of <strong>6</strong> booked seats checked in", cut.Markup);
        Assert.True(_events.GetRegistrations(1).Single(r => r.FullName == "Marcus Lee").IsCheckedIn);
    }

    [Fact]
    public void EventAttendance_UndoRemovesCheckIn()
    {
        var cut = Render<EventAttendance>(p => p.Add(c => c.Id, 1));

        cut.Find("button[aria-label='Undo check-in for Avery Johnson']").Click();

        Assert.False(_events.GetRegistrations(1).Single(r => r.FullName == "Avery Johnson").IsCheckedIn);
        Assert.NotNull(cut.Find("button[aria-label='Check in Avery Johnson']"));
    }
}
