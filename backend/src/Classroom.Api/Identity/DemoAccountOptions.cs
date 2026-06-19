namespace Classroom.Api.Identity;

/// <summary>
/// Configuration for the shared, writable demo account (design.md §9.2), bound from the
/// <c>DemoAccount</c> section. The account is seeded and periodically re-seeded to a rich state so a
/// reviewer's one-click "Try demo" always lands on something impressive.
/// </summary>
public sealed class DemoAccountOptions
{
    public const string SectionName = "DemoAccount";

    /// <summary>
    /// Master switch. When false, no demo account is seeded, the scheduled re-seed never runs, and the
    /// one-click demo login returns 404. The integration-test harness flips this off so the shared demo
    /// never pollutes other tests or races the background job.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Login identity of the demo teacher. Distinct from the real owner account.</summary>
    public string Email { get; set; } = "demo@classroom.local";

    public string DisplayName { get; set; } = "Demo Teacher";

    /// <summary>The demo password. Never surfaced to reviewers — they enter via the one-click login.</summary>
    public string Password { get; set; } = "DemoPassw0rd!";

    /// <summary>Kiosk exit PIN so reviewers can try (and leave) kiosk mode in the demo.</summary>
    public string KioskPin { get; set; } = "1234";

    /// <summary>How often the scheduled job resets the demo to its rich state.</summary>
    public double ReseedIntervalHours { get; set; } = 24;
}
