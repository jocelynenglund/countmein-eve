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

    [Fact]
    public void Unlimited_has_no_spots_left_and_no_waitlist()
    {
        var pitch = Fold.Of<Pitch>(Started(), Shown("p1", "Ana"))!;
        pitch.SpotsLeft.ShouldBeNull();
        pitch.WaitlistCount.ShouldBe(0);
    }

    [Fact]
    public void Spots_left_count_down_to_zero_then_the_waitlist_grows()
    {
        var pitch = Fold.Of<Pitch>(Started(capacity: 2), Shown("p1", "Ana"))!;
        pitch.SpotsLeft.ShouldBe(1);
        pitch.WaitlistCount.ShouldBe(0);

        pitch = Fold.Of<Pitch>(Started(capacity: 2), Shown("p1", "Ana"), Shown("p2", "Ben"), Shown("p3", "Dev"))!;
        pitch.SpotsLeft.ShouldBe(0);
        pitch.WaitlistCount.ShouldBe(1);
    }

    [Fact]
    public void When_a_spot_holder_leaves_the_waitlist_shrinks()
    {
        var pitch = Fold.Of<Pitch>(Started(capacity: 2),
            Shown("p1", "Ana"), Shown("p2", "Ben"), Shown("p3", "Dev"), Withdrawn("p1"))!;
        pitch.SpotsLeft.ShouldBe(0);
        pitch.WaitlistCount.ShouldBe(0);
    }
}
