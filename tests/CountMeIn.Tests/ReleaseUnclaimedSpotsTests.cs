using CountMeIn.Features;
using Shouldly;
using Wolverine.Marten;
using static CountMeIn.Tests.Scenarios;

namespace CountMeIn.Tests;

public class ReleaseUnclaimedSpotsTests
{
    static readonly DateTimeOffset Open = new(2026, 11, 14, 18, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset Close = new(2026, 11, 14, 20, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset ReleaseAt = Close.AddMinutes(-10);
    static readonly DateTimeOffset At = new(2026, 11, 14, 18, 30, 0, TimeSpan.Zero);

    static InterestCheckStarted Doors(int? capacity = 2) => Started(capacity: capacity, doorsOpen: Open, doorsClose: Close);
    static TurnedAwayAtDoor TriedAt(string personId, int minute) => new("c1", personId, At.AddMinutes(minute));

    static Events When(params object[] given) =>
        ReleaseUnclaimedSpotsHandler.Handle(new ReleaseUnclaimedSpots("c1"), Fold.Of<InterestCheck>(given));

    [Fact]
    public void Unclaimed_spots_go_to_those_who_tried_at_the_door_first_come_first_served() =>
        When(Doors(), Shown("p1", "Ana"), Shown("p2", "Ben"), Shown("p3", "Dev"), Shown("p4", "Eli"),
                TriedAt("p4", 1), TriedAt("p3", 2))
            .ShouldBeRelease(["p1", "p2"], ["p4", "p3"]);

    [Fact]
    public void Checked_in_people_keep_their_spot() =>
        When(Doors(), Shown("p1", "Ana"), Shown("p2", "Ben"), Shown("p3", "Dev"),
                new CheckedIn("c1", "p1", At), TriedAt("p3", 1))
            .ShouldBeRelease(["p2"], ["p3"]);

    [Fact]
    public void Waitlisted_people_who_never_tried_get_nothing() =>
        When(Doors(), Shown("p1", "Ana"), Shown("p2", "Ben"), Shown("p3", "Dev"))
            .ShouldBeRelease(["p1", "p2"], []);

    [Fact]
    public void Someone_who_tried_and_then_changed_their_mind_is_not_let_in() =>
        When(Doors(capacity: 1), Shown("p1", "Ana"), Shown("p2", "Ben"), TriedAt("p2", 1), Withdrawn("p2"))
            .ShouldBeRelease(["p1"], []);

    [Fact]
    public void Only_as_many_are_let_in_as_spots_were_released() =>
        When(Doors(capacity: 2), Shown("p1", "Ana"), Shown("p2", "Ben"), Shown("p3", "Dev"), Shown("p4", "Eli"),
                new CheckedIn("c1", "p1", At), TriedAt("p3", 1), TriedAt("p4", 2))
            .ShouldBeRelease(["p2"], ["p3"]);

    [Fact]
    public void Releasing_twice_does_nothing() =>
        When(Doors(), Shown("p1", "Ana"), new UnclaimedSpotsReleased("c1", ReleaseAt, ["p1"], [])).ShouldBeEmpty();

    [Fact]
    public void Unlimited_checks_have_nothing_to_release() =>
        When(Doors(capacity: null), Shown("p1", "Ana")).ShouldBeEmpty();

    [Fact]
    public void No_such_check_does_nothing() => When().ShouldBeEmpty();

    [Fact]
    public void The_release_is_ten_minutes_before_the_doors_close()
    {
        Fold.Of<InterestCheck>(Doors())!.ReleaseAt.ShouldBe(ReleaseAt);
    }

    [Fact]
    public void A_window_shorter_than_ten_minutes_releases_when_the_doors_open()
    {
        var short5 = Started(capacity: 2, doorsOpen: Open, doorsClose: Open.AddMinutes(5));
        Fold.Of<InterestCheck>(short5)!.ReleaseAt.ShouldBe(Open);
    }
}

static class ReleaseAssertions
{
    public static void ShouldBeRelease(this Events events, string[] lost, string[] letIn)
    {
        var e = events.ShouldHaveSingleItem().ShouldBeOfType<UnclaimedSpotsReleased>();
        e.At.ShouldBe(new DateTimeOffset(2026, 11, 14, 19, 50, 0, TimeSpan.Zero));
        e.LostTheirSpot.ShouldBe(lost);
        e.LetIn.ShouldBe(letIn);
    }
}
