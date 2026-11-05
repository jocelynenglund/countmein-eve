using JasperFx;
using Wolverine.Marten;

namespace CountMeIn.Features;

/// <summary>Scheduled when a check with an entry window starts, for ReleaseAt. Nobody sends it by hand.</summary>
public record ReleaseUnclaimedSpots([property: Identity] string CheckId);

[AggregateHandler]
public static class ReleaseUnclaimedSpotsHandler
{
    public static Events Handle(ReleaseUnclaimedSpots cmd, InterestCheck? check)
    {
        // Scheduled messages can arrive twice (see ScheduleReleases), so a second release is simply nothing.
        if (check is not { Released: false, Capacity: not null, ReleaseAt: { } at }) return [];

        var holders = check.PeopleIn.Where(p => !check.IsWaitlisted(p)).ToList();
        var lost = holders.Where(p => !check.CheckedIn.Contains(p)).ToArray();
        var letIn = check.TriedAtDoor.Where(p => check.PeopleIn.Contains(p) && check.IsWaitlisted(p)).Take(lost.Length).ToArray();

        return [new UnclaimedSpotsReleased(check.Id, at, lost, letIn)];
    }
}
