using Microsoft.Extensions.Options;

namespace Classroom.Api.Identity;

/// <summary>
/// The scheduled job that keeps the shared demo fresh (design.md §9.2): it seeds the demo to its rich
/// state once on startup, then re-seeds on a fixed interval so the demo always recovers from whatever
/// the last visitor did. A single-process <see cref="PeriodicTimer"/> is enough for this single-device
/// app — no external scheduler (Hangfire/Quartz) is warranted (CLAUDE.md: don't add scale machinery).
/// <para>
/// Disabled entirely when <see cref="DemoAccountOptions.Enabled"/> is false (the integration-test
/// harness flips it off so the demo never races other tests). Re-seed runs in its own DI scope and
/// swallows/logs failures so a transient DB hiccup never takes the host down.
/// </para>
/// </summary>
public sealed class DemoReseedService(
    IServiceProvider services,
    IOptions<DemoAccountOptions> options,
    ILogger<DemoReseedService> logger) : BackgroundService
{
    private readonly DemoAccountOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Demo account disabled; skipping demo seeding.");
            return;
        }

        // Initial seed on boot, then on the configured cadence.
        await ReseedAsync(stoppingToken);

        var interval = TimeSpan.FromHours(Math.Max(_options.ReseedIntervalHours, 1.0 / 60));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ReseedAsync(stoppingToken);
        }
    }

    private async Task ReseedAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DemoSeeder>();
            await seeder.ReseedAsync(cancellationToken);
            logger.LogInformation("Demo account re-seeded to its rich state.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Host is shutting down — expected, not an error.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to re-seed the demo account; will retry on the next tick.");
        }
    }
}
