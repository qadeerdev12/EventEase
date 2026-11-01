using System.ComponentModel.DataAnnotations;
using EventEase.Models;

namespace EventEase.Tests;

public class EventEditModelTests
{
    private static EventEditModel ValidModel() => new()
    {
        Name = "Team Offsite",
        Date = DateTime.Now.AddMonths(1),
        Location = "Denver",
    };

    private static List<ValidationResult> Validate(EventEditModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ValidModel_HasNoErrors()
    {
        Assert.Empty(Validate(ValidModel()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("  a  ")]
    public void InvalidName_IsRejected(string? name)
    {
        var model = ValidModel();
        model.Name = name;

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(EventEditModel.Name)));
    }

    [Fact]
    public void NameLongerThan100Characters_IsRejected()
    {
        var model = ValidModel();
        model.Name = new string('x', 101);

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(EventEditModel.Name)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingLocation_IsRejected(string? location)
    {
        var model = ValidModel();
        model.Location = location;

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(EventEditModel.Location)));
    }

    [Fact]
    public void MissingDate_IsRejected()
    {
        var model = ValidModel();
        model.Date = null;

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(EventEditModel.Date)));
    }

    [Fact]
    public void PastDate_IsRejected()
    {
        var model = ValidModel();
        model.Date = DateTime.Now.AddDays(-1);

        Assert.Contains(Validate(model), r => r.ErrorMessage == "Date must be in the future.");
    }

    [Fact]
    public void DateTooFarAhead_IsRejected()
    {
        var model = ValidModel();
        model.Date = DateTime.Now.AddYears(EventEditModel.MaxYearsAhead + 1);

        Assert.Contains(Validate(model), r => r.MemberNames.Contains(nameof(EventEditModel.Date)));
    }

    [Fact]
    public void ApplyTo_TrimsText()
    {
        var model = ValidModel();
        model.Name = "  Team Offsite  ";
        model.Location = "  Denver ";
        var ev = new Event();

        model.ApplyTo(ev);

        Assert.Equal("Team Offsite", ev.Name);
        Assert.Equal("Denver", ev.Location);
    }
}
