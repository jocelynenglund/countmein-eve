using CountMeIn.Features;
using Shouldly;
using static CountMeIn.Tests.Scenarios;

namespace CountMeIn.Tests;

public class DoorListTests
{
    static readonly DateTimeOffset At = new(2026, 11, 14, 18, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Arrived_expected_waitlist_and_turned_away()
    {
        var view = Fold.Of<DoorList>(Started(capacity: 2),
            Shown("p1", "Ana"), Shown("p2", "Ben"), Shown("p3", "Dev"), Shown("p4", "Eli"),
            new CheckedIn("c1", "p1", At), new TurnedAwayAtDoor("c1", "p3", At.AddMinutes(5)))!;

        view.CheckedIn.ShouldBe([new Arrival("Ana", At)]);
        view.NotArrivedYet.ShouldBe(["Ben"]);
        view.Waitlist.ShouldBe(["Dev", "Eli"]);
        view.TurnedAway.ShouldBe([new Arrival("Dev", At.AddMinutes(5))]);
    }

    [Fact]
    public void Someone_moving_up_from_the_waitlist_becomes_expected()
    {
        var view = Fold.Of<DoorList>(Started(capacity: 1), Shown("p1", "Ana"), Shown("p2", "Ben"), Withdrawn("p1"))!;
        view.NotArrivedYet.ShouldBe(["Ben"]);
        view.Waitlist.ShouldBeEmpty();
    }

    [Fact]
    public void Withdrawing_after_checking_in_takes_you_off_the_list()
    {
        var view = Fold.Of<DoorList>(Started(), Shown("p1", "Ana"), new CheckedIn("c1", "p1", At), Withdrawn("p1"))!;
        view.CheckedIn.ShouldBeEmpty();
    }
}
