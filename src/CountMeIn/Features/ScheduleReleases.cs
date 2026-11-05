using Marten;
using Wolverine;

namespace CountMeIn.Features;

/// <summary>
/// On startup, makes sure every check whose door night is still ahead (or under way) has its release scheduled.
/// Checks started before releases existed never got a scheduled message, and their stored InterestCheck
/// snapshots predate the fields the release reads (who tried at the door, and when), so they are
/// rebuilt from their events first. Safe to run on every start: releasing twice does nothing.
/// </summary>
public class ScheduleReleases(IServiceScopeFactory scopes, TimeProvider clock) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await using var session = store.QuerySession();
        // Filter on the stored fields only: older snapshots have no "Released" property to query.
        var withDoors = await session.Query<InterestCheck>().Where(c => c.DoorsClose != null).ToListAsync(ct);

        foreach (var check in withDoors.Where(c => !c.Released && c.Capacity != null && c.DoorsClose > clock.GetUtcNow()))
        {
            await store.Advanced.RebuildSingleStreamAsync<InterestCheck>(check.Id, ct);
            await bus.ScheduleAsync(new ReleaseUnclaimedSpots(check.Id), check.ReleaseAt!.Value);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
