using CountMeIn.Features;
using Shouldly;
using Wolverine.Marten;
using static CountMeIn.Tests.Scenarios;

namespace CountMeIn.Tests;

public class CountMeInTests
{
    static CountMeInCommand Cmd(string name = "Ana", string personId = "p1") => new("c1", personId, name, null, Nov4);
    static Events When(CountMeInCommand cmd, params object[] given) =>
        CountMeInCommandEndpoint.Handle(cmd, Fold.Of<InterestCheck>(given));
    static void Refused(string reason, CountMeInCommand cmd, params object[] given) =>
        Should.Throw<Refusal>(() => When(cmd, given)).Message.ShouldBe(reason);

    [Fact]
    public void Counting_yourself_in() =>
        When(Cmd(), Started()).ShouldBe([new InterestShown("c1", "p1", "Ana", null, Nov4)]);

    [Fact]
    public void You_need_a_name() => Refused("What's your name?", Cmd(name: ""), Started());

    [Fact]
    public void Counting_in_twice_is_refused() =>
        Refused("You're already counted in", Cmd(), Started(), Shown("p1", "Ana"));

    [Fact]
    public void Changed_your_mind_then_back_in() =>
        When(Cmd(), Started(), Shown("p1", "Ana"), Withdrawn("p1")).ShouldBe([new InterestShown("c1", "p1", "Ana", null, Nov4)]);

    [Fact]
    public void A_closed_check_takes_no_more_interest() =>
        Refused("This interest check has closed", Cmd("Ben", "p2"), Started(), Closed);

    [Fact]
    public void No_such_interest_check() => Refused("No such interest check", Cmd());
}
