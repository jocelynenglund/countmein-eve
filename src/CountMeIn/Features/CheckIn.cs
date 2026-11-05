using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;
using Wolverine.Marten;

namespace CountMeIn.Features;

public record CheckIn(string CheckId, string PersonId, DateTimeOffset At);

public static class CheckInEndpoint
{
    [WolverinePost("/checks/{checkId}/check-in")]
    public static (ProblemDetails, Events) Handle(CheckIn cmd, [WriteAggregate("checkId", Required = false)] InterestCheck? check)
    {
        if (check is null) throw new Refusal("No such interest check", 404);
        if (!check.PeopleIn.Contains(cmd.PersonId)) throw new Refusal("You're not on the list", 409);
        if (!check.DoorsAreOpen(cmd.At)) throw new Refusal("The doors aren't open", 409);

        // The refusal is also a fact: the owner wants to know who turned up without a spot.
        if (check.IsWaitlisted(cmd.PersonId))
            return (new ProblemDetails { Status = 409, Detail = "You're on the waitlist" },
                [new TurnedAwayAtDoor(check.Id, cmd.PersonId, cmd.At)]);

        if (check.CheckedIn.Contains(cmd.PersonId)) throw new Refusal("You're already checked in", 409);

        return (WolverineContinue.NoProblems, [new CheckedIn(check.Id, cmd.PersonId, cmd.At)]);
    }
}
