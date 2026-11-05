using Wolverine.Http;
using Wolverine.Marten;

namespace CountMeIn.Features;

public record ChangeMyMind(string CheckId, string PersonId, DateOnly On);

public static class ChangeMyMindEndpoint
{
    [WolverinePost("/checks/{checkId}/withdraw")]
    public static Events Handle(ChangeMyMind cmd, [WriteAggregate("checkId", Required = false)] InterestCheck? check)
    {
        if (check is null) throw new Refusal("No such interest check", 404);
        if (!check.PeopleIn.Contains(cmd.PersonId)) throw new Refusal("You're not counted in", 409);

        return [new InterestWithdrawn(check.Id, cmd.PersonId, cmd.On)];
    }
}
