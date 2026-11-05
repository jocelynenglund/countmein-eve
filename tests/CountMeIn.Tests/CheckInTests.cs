using CountMeIn.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Shouldly;
using static CountMeIn.Tests.Scenarios;

namespace CountMeIn.Tests;

public class CheckInTests
{
    static readonly DateTimeOffset Open = new(2026, 11, 14, 18, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset Close = new(2026, 11, 14, 20, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset Evening = new(2026, 11, 14, 18, 30, 0, TimeSpan.Zero);

    static InterestCheckStarted Doors(int? capacity = null) => Started(capacity: capacity, doorsOpen: Open, doorsClose: Close);

    static (IResult, Wolverine.Marten.Events) When(string personId, DateTimeOffset at, params object[] given) =>
        CheckInEndpoint.Handle(new CheckIn("c1", personId, at), Fold.Of<InterestCheck>(given));

    static void Refused(string reason, string personId, DateTimeOffset at, params object[] given) =>
        Should.Throw<Refusal>(() => When(personId, at, given)).Message.ShouldBe(reason);

    [Fact]
    public void Checking_in()
    {
        var (result, events) = When("p1", Evening, Doors(), Shown("p1", "Ana"));
        result.ShouldBeOfType<NoContent>();
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
        var (result, events) = When("p2", Evening, Doors(capacity: 1), Shown("p1", "Ana"), Shown("p2", "Ben"));
        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(409);
        problem.ProblemDetails.Detail.ShouldBe("You're on the waitlist");
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
        var (result, events) = When("p1", Evening, Doors(capacity: 1),
            Shown("p1", "Ana"), Shown("p2", "Ben"), Withdrawn("p1"), Shown("p1", "Ana"));
        result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(409);
        events.ShouldBe([new TurnedAwayAtDoor("c1", "p1", Evening)]);
    }

    [Fact]
    public void Already_checked_in() =>
        Refused("You're already checked in", "p1", Evening, Doors(), Shown("p1", "Ana"), new CheckedIn("c1", "p1", Evening));

    static readonly DateTimeOffset Late = Close.AddMinutes(-5);
    static UnclaimedSpotsReleased Released(string[] lost, string[] letIn) =>
        new("c1", Close.AddMinutes(-10), lost, letIn);

    [Fact]
    public void A_spot_holder_who_arrives_after_the_release_is_refused() =>
        Refused("Your spot went to the waitlist", "p1", Late, Doors(capacity: 1), Shown("p1", "Ana"), Shown("p2", "Ben"),
            Released(["p1"], []));

    [Fact]
    public void Someone_let_in_from_the_waitlist_can_check_in()
    {
        var (_, events) = When("p2", Late, Doors(capacity: 1), Shown("p1", "Ana"), Shown("p2", "Ben"),
            new TurnedAwayAtDoor("c1", "p2", Evening), Released(["p1"], ["p2"]));
        events.ShouldBe([new CheckedIn("c1", "p2", Late)]);
    }

    [Fact]
    public void Waitlisted_people_not_let_in_are_still_turned_away()
    {
        var (result, events) = When("p3", Late, Doors(capacity: 1), Shown("p1", "Ana"), Shown("p2", "Ben"), Shown("p3", "Dev"),
            new TurnedAwayAtDoor("c1", "p2", Evening), Released(["p1"], ["p2"]));
        result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(409);
        events.ShouldBe([new TurnedAwayAtDoor("c1", "p3", Late)]);
    }
}
