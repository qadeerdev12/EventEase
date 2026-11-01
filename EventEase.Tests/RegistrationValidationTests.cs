using System.ComponentModel.DataAnnotations;
using EventEase.Models;

namespace EventEase.Tests;

public class RegistrationValidationTests
{
    private static List<ValidationResult> Validate(Registration model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static void AssertInvalid(Registration model, string member) =>
        Assert.Contains(Validate(model), r => r.MemberNames.Contains(member));

    [Fact]
    public void ValidForm_HasNoErrors()
    {
        Assert.Empty(Validate(TestData.ValidForm()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]   // a field the user typed in and then cleared
    [InlineData("   ")]
    public void PhoneIsOptional(string? phone)
    {
        var form = TestData.ValidForm();
        form.Phone = phone;

        Assert.Empty(Validate(form));
    }

    [Theory]
    [InlineData("José O'Neil-Smith")]
    [InlineData("Dr. Ana María")]
    public void NamesWithAccentsAndPunctuation_AreAccepted(string name)
    {
        var form = TestData.ValidForm();
        form.FullName = name;

        Assert.Empty(Validate(form));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("J")]
    [InlineData("R2D2")]
    [InlineData("<script>")]
    public void InvalidName_IsRejected(string name)
    {
        var form = TestData.ValidForm();
        form.FullName = name;

        AssertInvalid(form, nameof(Registration.FullName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("jordan")]
    [InlineData("jordan@")]
    [InlineData("jordan@example")]
    [InlineData("jordan@@example.com")]
    [InlineData("jor dan@example.com")]
    public void InvalidEmail_IsRejected(string email)
    {
        var form = TestData.ValidForm();
        form.Email = email;

        AssertInvalid(form, nameof(Registration.Email));
    }

    [Theory]
    [InlineData("call me")]
    [InlineData("123456789012345678901234")]
    public void InvalidPhone_IsRejected(string phone)
    {
        var form = TestData.ValidForm();
        form.Phone = phone;

        AssertInvalid(form, nameof(Registration.Phone));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void SeatsOutOfRange_AreRejected(int seats)
    {
        var form = TestData.ValidForm(seats: seats);

        AssertInvalid(form, nameof(Registration.Seats));
    }

    [Fact]
    public void TermsMustBeAccepted()
    {
        var form = TestData.ValidForm();
        form.AcceptTerms = false;

        AssertInvalid(form, nameof(Registration.AcceptTerms));
    }

    [Fact]
    public void Normalize_TrimsAndLowercasesEmail()
    {
        var form = TestData.ValidForm(email: "  Jordan@Example.COM ");
        form.FullName = "  Jordan Rivera ";
        form.Phone = "   ";

        form.Normalize();

        Assert.Equal("Jordan Rivera", form.FullName);
        Assert.Equal("jordan@example.com", form.Email);
        Assert.Null(form.Phone);
    }
}
