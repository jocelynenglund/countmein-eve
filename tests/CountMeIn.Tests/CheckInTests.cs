using CountMeIn.Features;
using Shouldly;
using static CountMeIn.Tests.Scenarios;

namespace CountMeIn.Tests;

public class CheckInTests
{
    static readonly DateTimeOffset Open = new(2026, 11, 14, 18, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset Close = new(2026, 11, 14, 20, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset Evening = new(2026, 11, 14, 18, 30, 0, TimeSpan.Zero);

    static InterestCheckStarted Doors(int? capacity = null) => Started(capacity: capacity, doorsOpen: Open, doorsClose: Close);

    static (Microsoft.AspNetCore.Mvc.ProblemDetails, Wolverine.Marten.Events) When(string personId, DateTimeOffset at, params object[] given) =>
        CheckInEndpoint.Handle(new CheckIn("c1", personId, at), Fold.Of<InterestCheck>(given));

    static void Refused(string reason, string personId, DateTimeOffset at, params object[] given) =>
        Should.Throw<Refusal>(() => When(personId, at, given)).Message.ShouldBe(reason);

    [Fact]
    public void Checking_in()
    {
        var (problem, events) = When("p1", Evening, Doors(), Shown("p1", "Ana"));
        problem.Status.ShouldBeNull();
        events.ShouldBe([new CheckedIn("c1", "p1", Evening)]);
    }

    [Fact]
    public void Not_on_the_list() => Refused("You're not on the list", "p9", Evening, Doors(), Shown("p1", "Ana"));

    [Fact]
    public void Withdrawn_people_are_not_on_the_list() =>
        Refused("You're not on the list", "p1", Evening, Doors(), Shown("p1", "Ana"), Withdrawn("p1"));

    [Fact]
    public void Before_the_doors_open() =>
        Refused("The doors aren't open", "p1", Open.AddMinutes(-1), Doors(), Shown("p1", "Ana"));

    [Fact]
    public void After_the_doors_close() =>
        Refused("The doors aren't open", "p1", Close.AddMinutes(1), Doors(), Shown("p1", "Ana"));

    [Fact]
    public void Without_an_entry_window_check_in_is_not_possible() =>
        Refused("The doors aren't open", "p1", Evening, Started(), Shown("p1", "Ana"));

    [Fact]
    public void The_waitlisted_are_turned_away_and_it_is_recorded()
    {
        var (problem, events) = When("p2", Evening, Doors(capacity: 1), Shown("p1", "Ana"), Shown("p2", "Ben"));
        problem.Status.ShouldBe(409);
        problem.Detail.ShouldBe("You're on the waitlist");
        events.ShouldBe([new TurnedAwayAtDoor("c1", "p2", Evening)]);
    }

    [Fact]
    public void Someone_moved_up_from_the_waitlist_can_check_in()
    {
        var (_, events) = When("p2", Evening, Doors(capacity: 1), Shown("p1", "Ana"), Shown("p2", "Ben"), Withdrawn("p1"));
        events.ShouldBe([new CheckedIn("c1", "p2", Evening)]);
    }

    [Fact]
    public void Rejoining_puts_you_at_the_back()
    {
        var (problem, events) = When("p1", Evening, Doors(capacity: 1),
            Shown("p1", "Ana"), Shown("p2", "Ben"), Withdrawn("p1"), Shown("p1", "Ana"));
        problem.Status.ShouldBe(409);
        events.ShouldBe([new TurnedAwayAtDoor("c1", "p1", Evening)]);
    }

    [Fact]
    public void Already_checked_in() =>
        Refused("You're already checked in", "p1", Evening, Doors(), Shown("p1", "Ana"), new CheckedIn("c1", "p1", Evening));
}
