using EventEase.Models;
using EventEase.Services;

namespace EventEase.Tests;

public class EventServiceTests
{
    // Seeded event 3 ("Leadership Offsite") has capacity 40 with 1 seat already booked.
    private const int SmallEventId = 3;

    [Fact]
    public void Register_SavesACopyWithIdAndTimestamp()
    {
        var service = new EventService();
        var form = TestData.ValidForm(email: "New.Person@Example.com", seats: 2);

        var result = service.Register(1, form);

        Assert.True(result.Succeeded);
        var saved = result.Registration!;
        Assert.NotSame(form, saved);
        Assert.True(saved.Id > 0);
        Assert.Equal(1, saved.EventId);
        Assert.Equal("new.person@example.com", saved.Email);
        Assert.Contains(saved, service.GetRegistrations(1));

        form.Seats = 9; // changing the form afterwards must not change saved data
        Assert.Equal(2, service.GetRegistrations(1).Single(r => r.Id == saved.Id).Seats);
    }

    [Fact]
    public void Register_SameEmailTwice_IsRejectedIgnoringCase()
    {
        var service = new EventService();
        service.Register(1, TestData.ValidForm(email: "dup@example.com"));

        var result = service.Register(1, TestData.ValidForm(email: "DUP@example.com"));

        Assert.Equal(RegistrationStatus.AlreadyRegistered, result.Status);
    }

    [Fact]
    public void Register_SameEmailForDifferentEvents_IsAllowed()
    {
        var service = new EventService();
        service.Register(1, TestData.ValidForm());

        Assert.True(service.Register(2, TestData.ValidForm()).Succeeded);
    }

    [Fact]
    public void Register_MoreSeatsThanLeft_IsRejected()
    {
        var service = new EventService();
        for (var i = 0; i < 3; i++)
        {
            Assert.True(service.Register(SmallEventId, TestData.ValidForm($"p{i}@example.com", seats: 10)).Succeeded);
        }
        // 31 of 40 seats booked, 9 left.

        var result = service.Register(SmallEventId, TestData.ValidForm("late@example.com", seats: 10));

        Assert.Equal(RegistrationStatus.NotEnoughSeats, result.Status);
        Assert.Equal(9, service.GetById(SmallEventId)!.SeatsLeft);
    }

    [Fact]
    public void Register_ForPastEvent_IsRejected()
    {
        var afterAllEvents = new FixedTimeProvider(new DateTime(2030, 1, 1));
        var service = new EventService(time: afterAllEvents);

        Assert.Equal(RegistrationStatus.EventEnded, service.Register(1, TestData.ValidForm()).Status);
    }

    [Fact]
    public void Register_UnknownEvent_IsRejected()
    {
        Assert.Equal(RegistrationStatus.EventNotFound, new EventService().Register(999, TestData.ValidForm()).Status);
    }

    [Fact]
    public void CheckIn_UpdatesAttendanceSummary()
    {
        var service = new EventService();
        var registration = service.Register(2, TestData.ValidForm(seats: 3)).Registration!;

        Assert.True(service.SetCheckedIn(2, registration.Id, true));

        var summary = service.GetAttendance().Single(a => a.Event.Id == 2);
        Assert.Equal(2, summary.Registrations);         // seeded Sofia + new registration
        Assert.Equal(5, summary.SeatsBooked);           // 2 + 3
        Assert.Equal(3, summary.SeatsCheckedIn);
        Assert.Equal(0.6, summary.AttendanceRate!.Value, precision: 3);
    }

    [Fact]
    public void UndoCheckIn_ClearsTimestamp()
    {
        var service = new EventService();
        var registration = service.Register(2, TestData.ValidForm()).Registration!;
        service.SetCheckedIn(2, registration.Id, true);

        service.SetCheckedIn(2, registration.Id, false);

        Assert.False(service.GetRegistrations(2).Single(r => r.Id == registration.Id).IsCheckedIn);
    }

    [Fact]
    public void CheckIn_UnknownRegistration_ReturnsFalse()
    {
        Assert.False(new EventService().SetCheckedIn(1, 999, true));
    }

    [Fact]
    public void AttendanceRate_IsNullWithoutRegistrations()
    {
        var summary = new EventService().GetAttendance().Single(a => a.Event.Id == 4);

        Assert.Null(summary.AttendanceRate);
    }

    [Fact]
    public void Changed_IsRaisedForRegistrationAndCheckIn()
    {
        var service = new EventService();
        var count = 0;
        service.Changed += () => count++;

        var registration = service.Register(1, TestData.ValidForm()).Registration!;
        service.SetCheckedIn(1, registration.Id, true);

        Assert.Equal(2, count);
    }

    [Fact]
    public void GetRegistrationsForEmail_FindsAcrossEvents()
    {
        var service = new EventService();
        service.Register(2, TestData.ValidForm());
        service.Register(1, TestData.ValidForm());

        var mine = service.GetRegistrationsForEmail(" JORDAN@example.com ");

        Assert.Equal([1, 2], mine.Select(m => m.Event.Id));
    }

    [Fact]
    public async Task ConcurrentRegistrations_NeverOverbook()
    {
        var service = new EventService();
        // 39 seats left on event 3; 100 people try to book 1 seat at the same time.
        var results = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(i => Task.Run(() => service.Register(SmallEventId, TestData.ValidForm($"c{i}@example.com")))));

        Assert.Equal(39, results.Count(r => r.Succeeded));
        Assert.Equal(0, service.GetById(SmallEventId)!.SeatsLeft);
    }
}
