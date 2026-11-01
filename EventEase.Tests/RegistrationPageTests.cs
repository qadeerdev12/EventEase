using Bunit;
using EventEase.Components.Pages;
using EventEase.Models;
using EventEase.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventEase.Tests;

public class RegistrationPageTests : BunitContext
{
    private readonly EventService _events = new();
    private readonly InMemorySessionStore _store = new();

    public RegistrationPageTests()
    {
        Services.AddLogging();
        Services.AddSingleton(_events);
        Services.AddSingleton<ISessionStore>(_store);
        Services.AddScoped<UserSession>();
    }

    private UserSession Session => Services.GetRequiredService<UserSession>();

    private IRenderedComponent<EventRegistration> RenderPage(int eventId = 1) =>
        Render<EventRegistration>(p => p.Add(c => c.Id, eventId));

    private static void Fill(IRenderedComponent<EventRegistration> cut, string name, string email,
        string phone = "", string seats = "1", bool terms = true)
    {
        cut.Find("#fullName").Change(name);
        cut.Find("#email").Change(email);
        cut.Find("#phone").Change(phone);
        cut.Find("#seats").Change(seats);
        cut.Find("#terms").Change(terms);
    }

    [Fact]
    public void SubmittingEmptyForm_ShowsSummaryAndFieldErrors()
    {
        var cut = RenderPage();

        cut.Find("form").Submit();

        Assert.Contains("Please fix the following:", cut.Markup);
        Assert.Contains("Please enter your name.", cut.Markup);
        Assert.Contains("Please enter your email.", cut.Markup);
        Assert.Contains("Please accept the event terms to continue.", cut.Markup);
        Assert.Equal(3, _events.GetRegistrations(1).Count); // nothing new saved
    }

    [Fact]
    public void InvalidEmailAndSeats_ShowSpecificMessages()
    {
        var cut = RenderPage();
        Fill(cut, "Jordan Rivera", "jordan@example", seats: "25");

        cut.Find("form").Submit();

        Assert.Contains(Registration.EmailError, cut.Markup);
        Assert.Contains("You can reserve 1 to 10 seats.", cut.Markup);
    }

    [Fact]
    public void NonNumericSeats_ShowsParsingError()
    {
        var cut = RenderPage();
        Fill(cut, "Jordan Rivera", "jordan@example.com", seats: "two");

        cut.Find("form").Submit();

        Assert.Contains("Please enter a whole number of seats.", cut.Markup);
    }

    [Fact]
    public void ValidSubmission_ConfirmsAndStartsSession()
    {
        var cut = RenderPage();
        Fill(cut, "Jordan Rivera", "Jordan@Example.com", phone: "555-123-4567", seats: "2");

        cut.Find("form").Submit();

        Assert.Contains("You're registered!", cut.Markup);
        Assert.Contains("2 seats", cut.Markup);
        Assert.Contains(_events.GetRegistrations(1), r => r.Email == "jordan@example.com" && r.Seats == 2);
        Assert.True(Session.IsIdentified);
        Assert.Equal("jordan@example.com", Session.Email);
    }

    [Fact]
    public void DuplicateRegistration_ShowsFriendlyError()
    {
        _events.Register(1, TestData.ValidForm(email: "jordan@example.com"));
        var cut = RenderPage();
        Fill(cut, "Jordan Rivera", "jordan@example.com");

        cut.Find("form").Submit();

        Assert.Contains("is already registered for this event", cut.Markup);
        Assert.DoesNotContain("You're registered!", cut.Markup);
    }

    [Fact]
    public void TooManySeats_ShowsSeatsLeft()
    {
        for (var i = 0; i < 3; i++) _events.Register(3, TestData.ValidForm($"p{i}@example.com", seats: 10));
        var cut = RenderPage(3); // 9 seats left
        Fill(cut, "Jordan Rivera", "jordan@example.com", seats: "10");

        cut.Find("form").Submit();

        Assert.Contains("Only 9 seats left", cut.Markup);
    }

    [Fact]
    public void SoldOutEvent_HidesForm()
    {
        for (var i = 0; i < 3; i++) _events.Register(3, TestData.ValidForm($"p{i}@example.com", seats: 10));
        _events.Register(3, TestData.ValidForm("last@example.com", seats: 9));

        var cut = RenderPage(3);

        Assert.Contains("This event is sold out.", cut.Markup);
        Assert.Empty(cut.FindAll("form"));
    }

    [Fact]
    public async Task ReturningVisitor_IsPrefilledFromSession()
    {
        await Session.IdentifyAsync("Jordan Rivera", "jordan@example.com");

        var cut = RenderPage();

        Assert.Equal("Jordan Rivera", cut.Find("#fullName").GetAttribute("value"));
        Assert.Equal("jordan@example.com", cut.Find("#email").GetAttribute("value"));
    }
}
