using Bunit;
using EventEase.Components.Shared;
using EventEase.Models;

namespace EventEase.Tests;

public class EventCardTests : BunitContext
{
    private static Event SampleEvent() => new()
    {
        Id = 7,
        Name = "Design Summit",
        Date = DateTime.Now.AddMonths(2),
        Location = "Boston",
        Capacity = 50,
    };

    private IRenderedComponent<EventCard> RenderEditable(Event ev, Action<Event> onChanged) =>
        Render<EventCard>(p => p
            .Add(c => c.Event, ev)
            .Add(c => c.AllowEdit, true)
            .Add(c => c.EventChanged, onChanged));

    [Fact]
    public void ShowsNameDateAndLocation()
    {
        var cut = Render<EventCard>(p => p.Add(c => c.Event, SampleEvent()));

        Assert.Contains("Design Summit", cut.Markup);
        Assert.Contains("Boston", cut.Markup);
        Assert.NotNull(cut.Find("a[href='events/7']"));
        Assert.NotNull(cut.Find("a[href='events/7/register']"));
    }

    [Fact]
    public void NullEvent_RendersFallbackInsteadOfCrashing()
    {
        var cut = Render<EventCard>(p => p.Add(c => c.Event, (Event?)null));

        Assert.Contains("Event details are unavailable", cut.Markup);
    }

    [Fact]
    public void EmptyFields_ShowFallbackText()
    {
        var cut = Render<EventCard>(p => p.Add(c => c.Event, new Event { Id = 1, Date = default }));

        Assert.Contains("Untitled event", cut.Markup);
        Assert.Contains("Date to be announced", cut.Markup);
        Assert.Contains("Location to be announced", cut.Markup);
    }

    [Fact]
    public void SavingEmptyName_ShowsErrorAndDoesNotRaiseEventChanged()
    {
        var ev = SampleEvent();
        Event? received = null;
        var cut = RenderEditable(ev, e => received = e);

        cut.Find("button.btn-outline-secondary").Click();
        cut.Find("#name-7").Change("");
        cut.Find("form").Submit();

        Assert.Null(received);
        Assert.Equal("Design Summit", ev.Name);
        Assert.Contains("Event name is required.", cut.Markup);
    }

    [Fact]
    public void SavingInvalidDate_ShowsParsingError()
    {
        var ev = SampleEvent();
        Event? received = null;
        var cut = RenderEditable(ev, e => received = e);

        cut.Find("button.btn-outline-secondary").Click();
        cut.Find("#date-7").Change("not-a-date");
        cut.Find("form").Submit();

        Assert.Null(received);
        Assert.Contains("Please enter a valid date and time.", cut.Markup);
    }

    [Fact]
    public void SavingPastDate_ShowsError()
    {
        Event? received = null;
        var cut = RenderEditable(SampleEvent(), e => received = e);

        cut.Find("button.btn-outline-secondary").Click();
        cut.Find("#date-7").Change(DateTime.Now.AddDays(-3).ToString("yyyy-MM-ddTHH:mm"));
        cut.Find("form").Submit();

        Assert.Null(received);
        Assert.Contains("Date must be in the future.", cut.Markup);
    }

    [Fact]
    public void SavingValidData_RaisesEventChangedWithTrimmedValues()
    {
        var ev = SampleEvent();
        Event? received = null;
        var cut = RenderEditable(ev, e => received = e);

        cut.Find("button.btn-outline-secondary").Click();
        cut.Find("#name-7").Change("  Design Summit 2027  ");
        cut.Find("#location-7").Change(" Cambridge ");
        cut.Find("form").Submit();

        Assert.NotNull(received);
        Assert.Equal(7, received.Id);
        Assert.Equal("Design Summit 2027", received.Name);
        Assert.Equal("Cambridge", received.Location);
        // The original object is untouched; the parent decides what to do with the update.
        Assert.Equal("Design Summit", ev.Name);
    }

    [Fact]
    public void Cancel_DiscardsChanges()
    {
        Event? received = null;
        var cut = RenderEditable(SampleEvent(), e => received = e);

        cut.Find("button.btn-outline-secondary").Click();
        cut.Find("#name-7").Change("Something else");
        cut.Find("button.btn-link").Click();

        Assert.Null(received);
        Assert.Contains("Design Summit", cut.Markup);
        Assert.Empty(cut.FindAll("form"));
    }
}
