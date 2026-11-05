using CountMeIn.Features;
using Shouldly;
using static CountMeIn.Tests.Scenarios;

namespace CountMeIn.Tests;

public class HowItsGoingTests
{
    [Fact]
    public void Interest_per_day_and_whos_in()
    {
        var nov6 = new DateOnly(2026, 11, 6);
        var nov7 = new DateOnly(2026, 11, 7);
        var view = Fold.Of<HowItsGoing>(
            Started(), Shown("p1", "Ana", Nov4), Shown("p2", "Ben", Nov4), Shown("p3", "Dev", nov6), Withdrawn("p2", nov7))!;

        view.Count.ShouldBe(2);
        view.Goal.ShouldBe(10);
        view.GoalReached.ShouldBeFalse();
        view.ByDay.ShouldBe([new DayTally(Nov4, 2, 0), new DayTally(nov6, 1, 0), new DayTally(nov7, 0, 1)]);
        view.People.Select(p => p.Name).ShouldBe(["Ana", "Dev"]);
    }
}
