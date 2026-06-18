namespace Classroom.Domain.Identity;

/// <summary>
/// The rules for a kiosk exit PIN (design.md §5.4): a short numeric code a teacher can type quickly
/// to leave kiosk mode. Pure policy so it can be unit-tested and shared by the validator and the
/// set-PIN endpoint.
/// </summary>
public static class KioskPinPolicy
{
    public const int MinLength = 4;
    public const int MaxLength = 6;

    /// <summary>Whether <paramref name="pin"/> is a valid PIN: digits only, 4–6 characters.</summary>
    public static bool IsValidFormat(string? pin) =>
        pin is not null
        && pin.Length >= MinLength
        && pin.Length <= MaxLength
        && pin.All(char.IsAsciiDigit);
}
