using CountMeIn.Features;
using Shouldly;
using Wolverine.Marten;
using static CountMeIn.Tests.Scenarios;

namespace CountMeIn.Tests;

public class SetVenueTests
{
    static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset RevealAt = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    static Events When(SetVenue cmd, params object[] given) => SetVenueEndpoint.Handle(cmd, Fold.Of<InterestCheck>(given));
    static SetVenue Cmd(string key = "k1", string venue = "The Library, Main St 4", DateTimeOffset? revealAt = null, DateTimeOffset? at = null) =>
        new("c1", key, venue, revealAt ?? RevealAt, at ?? Now);
    static void Refused(string reason, SetVenue cmd, params object[] given) =>
        Should.Throw<Refusal>(() => When(cmd, given)).Message.ShouldBe(reason);

    [Fact]
    public void Setting_the_venue() =>
        When(Cmd(), Started()).ShouldBe([new VenueSet("c1", "The Library, Main St 4", RevealAt, Now)]);

    [Fact]
    public void Changing_the_venue_before_it_is_revealed() =>
        When(Cmd(venue: "The Cafe"), Started(), new VenueSet("c1", "The Library", RevealAt, Now.AddDays(-1)))
            .ShouldBe([new VenueSet("c1", "The Cafe", RevealAt, Now)]);

    [Fact]
    public void Only_the_owner_can_set_it() =>
        Refused("Only the owner can set the venue", Cmd(key: "wrong"), Started());

    [Fact]
    public void The_venue_is_required() => Refused("Where is it?", Cmd(venue: " "), Started());

    [Fact]
    public void After_the_reveal_it_is_final() =>
        Refused("The venue has already been revealed", Cmd(venue: "The Cafe", at: RevealAt),
            Started(), new VenueSet("c1", "The Library", RevealAt, Now.AddDays(-1)));

    [Fact]
    public void A_reveal_time_that_has_already_passed_is_refused() =>
        Refused("The venue has already been revealed", Cmd(revealAt: Now.AddMinutes(-1)), Started());

    [Fact]
    public void No_such_interest_check() => Refused("No such interest check", Cmd());
}
