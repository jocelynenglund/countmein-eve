using CountMeIn.Features;
using Shouldly;
using static CountMeIn.Tests.Scenarios;

namespace CountMeIn.Tests;

public class VenueRevealTests
{
    static readonly DateTimeOffset RevealAt = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset After = RevealAt.AddMinutes(1);
    static readonly VenueSet Venue = new("c1", "The Library, Main St 4", RevealAt, RevealAt.AddDays(-3));

    static VenueAnswer Ask(string personId, DateTimeOffset now, params object[] given) =>
        VenueEndpoint.Reveal(Fold.Of<InterestCheck>(given)!, personId, now);

    [Fact]
    public void Before_the_reveal_nobody_sees_it()
    {
        var answer = Ask("p1", RevealAt.AddMinutes(-1), Started(capacity: 2), Shown("p1", "Ana"), Venue);
        answer.Venue.ShouldBeNull();
        answer.RevealAt.ShouldBe(RevealAt);
    }

    [Fact]
    public void A_spot_holder_sees_it_from_the_reveal() =>
        Ask("p1", RevealAt, Started(capacity: 2), Shown("p1", "Ana"), Venue).Venue.ShouldBe("The Library, Main St 4");

    [Fact]
    public void Everyone_counted_in_sees_it_when_there_is_no_capacity() =>
        Ask("p1", After, Started(), Shown("p1", "Ana"), Venue).Venue.ShouldNotBeNull();

    [Fact]
    public void The_waitlisted_do_not_see_it() =>
        Ask("p2", After, Started(capacity: 1), Shown("p1", "Ana"), Shown("p2", "Ben"), Venue).Venue.ShouldBeNull();

    [Fact]
    public void Someone_moving_up_the_waitlist_sees_it() =>
        Ask("p2", After, Started(capacity: 1), Shown("p1", "Ana"), Shown("p2", "Ben"), Withdrawn("p1"), Venue)
            .Venue.ShouldNotBeNull();

    [Fact]
    public void Someone_let_in_from_the_waitlist_sees_it() =>
        Ask("p2", After, Started(capacity: 1), Shown("p1", "Ana"), Shown("p2", "Ben"),
            new UnclaimedSpotsReleased("c1", RevealAt.AddHours(-1), ["p1"], ["p2"]), Venue).Venue.ShouldNotBeNull();

    [Fact]
    public void Someone_who_lost_their_spot_does_not_see_it() =>
        Ask("p1", After, Started(capacity: 1), Shown("p1", "Ana"), Shown("p2", "Ben"),
            new UnclaimedSpotsReleased("c1", RevealAt.AddHours(-1), ["p1"], ["p2"]), Venue).Venue.ShouldBeNull();

    [Fact]
    public void Someone_who_changed_their_mind_does_not_see_it() =>
        Ask("p1", After, Started(capacity: 2), Shown("p1", "Ana"), Withdrawn("p1"), Venue).Venue.ShouldBeNull();

    [Fact]
    public void Someone_who_never_counted_in_does_not_see_it() =>
        Ask("p9", After, Started(capacity: 2), Shown("p1", "Ana"), Venue).Venue.ShouldBeNull();
}
