using Wolverine.Http;
using Wolverine.Marten;

namespace CountMeIn.Features;

public record CloseInterestCheck(string CheckId, string OwnerKey, DateOnly On);

public static class CloseInterestCheckEndpoint
{
    [WolverinePost("/checks/{checkId}/close")]
    public static Events Handle(CloseInterestCheck cmd, [WriteAggregate("checkId", Required = false)] InterestCheck? check)
    {
        if (check is null) throw new Refusal("No such interest check", 404);
        if (!OwnerKey.Matches(cmd.OwnerKey, check.OwnerKeyHash)) throw new Refusal("Only the owner can close this", 403);
        if (check.Closed) throw new Refusal("It's already closed", 409);

        return [new InterestCheckClosed(check.Id, cmd.On)];
    }
}
