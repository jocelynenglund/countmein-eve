using Marten;

namespace CountMeIn.Features;

/// <summary>
/// On startup, rebuilds the InterestCheck and DoorList snapshots of every check whose venue is not yet revealed
/// (or not yet set). Snapshots stored before early birds shipped never recorded who used Change My Mind, and
/// "ever changed their mind" can't be recovered from the people list, only from the events. Safe on every start.
/// </summary>
public class RebuildStaleSnapshots(IServiceScopeFactory scopes, TimeProvider clock) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        await using var session = store.QuerySession();
        var checks = await session.Query<InterestCheck>().ToListAsync(ct);

        foreach (var check in checks.Where(c => c.RevealAt is null || c.RevealAt > clock.GetUtcNow()))
        {
            await store.Advanced.RebuildSingleStreamAsync<InterestCheck>(check.Id, ct);
            await store.Advanced.RebuildSingleStreamAsync<DoorList>(check.Id, ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
