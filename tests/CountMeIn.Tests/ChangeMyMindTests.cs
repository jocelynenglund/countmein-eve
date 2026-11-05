using CountMeIn.Features;
using Shouldly;
using Wolverine.Marten;
using static CountMeIn.Tests.Scenarios;

namespace CountMeIn.Tests;

public class ChangeMyMindTests
{
    static readonly ChangeMyMind Cmd = new("c1", "p1", Nov4);

    [Fact]
    public void Changing_your_mind() =>
        ChangeMyMindEndpoint.Handle(Cmd, Fold.Of<InterestCheck>(Started(), Shown("p1", "Ana")))
            .ShouldBe([new InterestWithdrawn("c1", "p1", Nov4)]);

    [Fact]
    public void You_cant_take_back_what_you_never_gave() =>
        Should.Throw<Refusal>(() => ChangeMyMindEndpoint.Handle(Cmd, Fold.Of<InterestCheck>(Started())))
            .Message.ShouldBe("You're not counted in");
}
