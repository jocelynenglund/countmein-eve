using CountMeIn.Features;
using Shouldly;
using static CountMeIn.Tests.Scenarios;

namespace CountMeIn.Tests;

public class PitchTests
{
    [Fact]
    public void The_pitch_counts_whos_in()
    {
        var pitch = Fold.Of<Pitch>(Started(goal: 3), Shown("p1", "Ana"), Shown("p2", "Ben"), Withdrawn("p1"))!;
        pitch.Title.ShouldBe("Board games");
        pitch.Goal.ShouldBe(3);
        pitch.Count.ShouldBe(1);
        pitch.GoalReached.ShouldBeFalse();
        pitch.Open.ShouldBeTrue();
    }

    [Fact]
    public void Reaching_the_goal()
    {
        var pitch = Fold.Of<Pitch>(Started(goal: 2), Shown("p1", "Ana"), Shown("p2", "Ben"))!;
        pitch.Count.ShouldBe(2);
        pitch.GoalReached.ShouldBeTrue();
    }

    [Fact]
    public void A_closed_check_shows_as_closed()
    {
        var closesOn = new DateOnly(2026, 11, 10);
        var pitch = Fold.Of<Pitch>(Started(closesOn: closesOn), Shown("p1", "Ana"), Closed)!;
        pitch.Count.ShouldBe(1);
        pitch.Open.ShouldBeFalse();
        pitch.ClosesOn.ShouldBe(closesOn);
    }
}
