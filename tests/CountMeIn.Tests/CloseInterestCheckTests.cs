using CountMeIn.Features;
using Shouldly;
using Wolverine.Marten;
using static CountMeIn.Tests.Scenarios;

namespace CountMeIn.Tests;

public class CloseInterestCheckTests
{
    static Events When(string key, params object[] given) =>
        CloseInterestCheckEndpoint.Handle(new CloseInterestCheck("c1", key, Nov4), Fold.Of<InterestCheck>(given));

    [Fact]
    public void Closing_an_interest_check() => When("k1", Started()).ShouldBe([Closed]);

    [Fact]
    public void Only_the_owner_can_close_it() =>
        Should.Throw<Refusal>(() => When("wrong", Started())).Message.ShouldBe("Only the owner can close this");

    [Fact]
    public void Closing_twice_is_refused() =>
        Should.Throw<Refusal>(() => When("k1", Started(), Closed)).Message.ShouldBe("It's already closed");
}
