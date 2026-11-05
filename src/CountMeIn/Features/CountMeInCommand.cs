using Wolverine.Http;
using Wolverine.Marten;

namespace CountMeIn.Features;

public record CountMeInCommand(string CheckId, string PersonId, string Name, string? Email, DateOnly On);

public static class CountMeInCommandEndpoint
{
    [WolverinePost("/checks/{checkId}/interest")]
    public static Events Handle(CountMeInCommand cmd, [WriteAggregate("checkId", Required = false)] InterestCheck? check)
    {
        if (check is null) throw new Refusal("No such interest check", 404);
        if (check.Closed) throw new Refusal("This interest check has closed", 409);
        if (string.IsNullOrWhiteSpace(cmd.Name)) throw new Refusal("What's your name?");
        if (check.PeopleIn.Contains(cmd.PersonId)) throw new Refusal("You're already counted in", 409);

        return [new InterestShown(check.Id, cmd.PersonId, cmd.Name.Trim(), cmd.Email, cmd.On)];
    }
}
