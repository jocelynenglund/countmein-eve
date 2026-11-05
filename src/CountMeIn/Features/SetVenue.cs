using Wolverine.Http;
using Wolverine.Marten;

namespace CountMeIn.Features;

public record SetVenue(string CheckId, string OwnerKey, string Venue, DateTimeOffset RevealAt, DateTimeOffset At);

public static class SetVenueEndpoint
{
    [WolverinePost("/checks/{checkId}/venue")]
    public static Events Handle(SetVenue cmd, [WriteAggregate("checkId", Required = false)] InterestCheck? check)
    {
        if (check is null) throw new Refusal("No such interest check", 404);
        if (!OwnerKey.Matches(cmd.OwnerKey, check.OwnerKeyHash)) throw new Refusal("Only the owner can set the venue", 403);
        if (string.IsNullOrWhiteSpace(cmd.Venue)) throw new Refusal("Where is it?");
        if (cmd.At >= (check.RevealAt ?? cmd.RevealAt) || cmd.At >= cmd.RevealAt)
            throw new Refusal("The venue has already been revealed", 409);

        return [new VenueSet(check.Id, cmd.Venue.Trim(), cmd.RevealAt, cmd.At)];
    }
}
