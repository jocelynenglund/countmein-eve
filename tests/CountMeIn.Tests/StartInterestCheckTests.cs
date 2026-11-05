using CountMeIn.Features;
using Shouldly;
using static CountMeIn.Tests.Scenarios;

namespace CountMeIn.Tests;

public class StartInterestCheckTests
{
    static StartInterestCheck Cmd(string title = "Saturday board-game club", int goal = 10, DateOnly? closesOn = null,
        int? capacity = null, DateTimeOffset? doorsOpen = null, DateTimeOffset? doorsClose = null) =>
        new("c1", title, "Monthly games at the library", goal, closesOn, "k1", Nov4, capacity, doorsOpen, doorsClose);

    static readonly DateTimeOffset Open = new(2026, 11, 14, 18, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset Close = new(2026, 11, 14, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Starting_an_interest_check() =>
        StartInterestCheckEndpoint.Decide(Cmd()).ShouldBe(
            new InterestCheckStarted("c1", "Saturday board-game club", "Monthly games at the library", 10, null, OwnerKey.Hash("k1")));

    [Fact]
    public void An_interest_check_needs_a_title() =>
        Should.Throw<Refusal>(() => StartInterestCheckEndpoint.Decide(Cmd(title: " "))).Message.ShouldBe("What's the idea called?");

    [Fact]
    public void The_goal_is_at_least_1() =>
        Should.Throw<Refusal>(() => StartInterestCheckEndpoint.Decide(Cmd(goal: 0)))
            .Message.ShouldBe("How many people would make it worth doing? At least 1.");

    [Fact]
    public void A_closing_date_in_the_past_is_refused() =>
        Should.Throw<Refusal>(() => StartInterestCheckEndpoint.Decide(Cmd(closesOn: new DateOnly(2026, 11, 1))))
            .Message.ShouldBe("The closing date has already passed");

    [Fact]
    public void The_owner_key_is_never_stored_only_its_hash() =>
        StartInterestCheckEndpoint.Decide(Cmd()).OwnerKeyHash.ShouldNotBe("k1");

    [Fact]
    public void Starting_with_a_capacity_and_an_entry_window() =>
        StartInterestCheckEndpoint.Decide(Cmd(capacity: 20, doorsOpen: Open, doorsClose: Close)).ShouldBe(
            new InterestCheckStarted("c1", "Saturday board-game club", "Monthly games at the library", 10, null,
                OwnerKey.Hash("k1"), 20, Open, Close));

    [Fact]
    public void Capacity_is_at_least_1() =>
        Should.Throw<Refusal>(() => StartInterestCheckEndpoint.Decide(Cmd(capacity: 0)))
            .Message.ShouldBe("Capacity must be at least 1");

    [Fact]
    public void The_doors_must_open_before_they_close() =>
        Should.Throw<Refusal>(() => StartInterestCheckEndpoint.Decide(Cmd(doorsOpen: Close, doorsClose: Open)))
            .Message.ShouldBe("The doors must open before they close");

    [Fact]
    public void The_entry_window_needs_both_ends() =>
        Should.Throw<Refusal>(() => StartInterestCheckEndpoint.Decide(Cmd(doorsOpen: Open)))
            .Message.ShouldBe("The entry window needs both doorsOpen and doorsClose");
}
