namespace EventEase.Models;

public enum RegistrationStatus
{
    Success,
    EventNotFound,
    EventEnded,
    NotEnoughSeats,
    AlreadyRegistered,
}

public record RegistrationResult(RegistrationStatus Status, Registration? Registration = null)
{
    public bool Succeeded => Status == RegistrationStatus.Success;
}

// One row of the attendance tracker.
public record AttendanceSummary(Event Event, int Registrations, int SeatsBooked, int SeatsCheckedIn)
{
    public int Capacity => Event.Capacity;

    // Share of booked seats that have checked in; null until someone registers.
    public double? AttendanceRate => SeatsBooked == 0 ? null : (double)SeatsCheckedIn / SeatsBooked;

    public double FillRate => Capacity == 0 ? 0 : (double)SeatsBooked / Capacity;
}

// A registration paired with its event, for the "My events" page.
public record UserRegistration(Event Event, Registration Registration);
