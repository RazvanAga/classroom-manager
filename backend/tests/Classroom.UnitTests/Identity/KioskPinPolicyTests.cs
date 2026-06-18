using Classroom.Domain.Identity;

namespace Classroom.UnitTests.Identity;

public class KioskPinPolicyTests
{
    [Theory]
    [InlineData("1234")]
    [InlineData("0000")]
    [InlineData("12345")]
    [InlineData("123456")]
    public void Accepts_four_to_six_digit_pins(string pin) =>
        Assert.True(KioskPinPolicy.IsValidFormat(pin));

    [Theory]
    [InlineData(null)]      // unset
    [InlineData("")]        // empty
    [InlineData("123")]     // too short
    [InlineData("1234567")] // too long
    [InlineData("12a4")]    // non-digit
    [InlineData("12 4")]    // whitespace
    [InlineData("12.4")]    // punctuation
    [InlineData("१२३४")]    // non-ASCII digits
    public void Rejects_anything_that_is_not_four_to_six_ascii_digits(string? pin) =>
        Assert.False(KioskPinPolicy.IsValidFormat(pin));
}
