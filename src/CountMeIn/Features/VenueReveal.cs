using Wolverine.Http;
using Wolverine.Http.Marten;

namespace CountMeIn.Features;

/// <summary>Venue is null until the reveal, and for anyone who doesn't hold a spot at that moment.</summary>
public record VenueAnswer(string? Venue, DateTimeOffset RevealAt);

public static class VenueEndpoint
{
    [WolverineGet("/checks/{checkId}/venue")]
    public static VenueAnswer Get([Document("checkId")] InterestCheck check, string personId, TimeProvider clock) =>
        Reveal(check, personId, clock.GetUtcNow());

    public static VenueAnswer Reveal(InterestCheck check, string personId, DateTimeOffset now)
    {
        if (check.Venue is null || check.RevealAt is not { } revealAt) throw new Refusal("No venue yet", 404);
        var earlyBird = now >= revealAt.AddHours(-1) && !check.ChangedMind.Contains(personId);
        var sees = check.HoldsSpot(personId) && (now >= revealAt || earlyBird);
        return new VenueAnswer(sees ? check.Venue : null, revealAt);
    }
}
