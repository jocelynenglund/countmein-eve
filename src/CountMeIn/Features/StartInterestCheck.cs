using Marten.Events;
using Wolverine.Http;
using Wolverine.Marten;

namespace CountMeIn.Features;

public record StartInterestCheck(string CheckId, string Title, string? Pitch, int Goal, DateOnly? ClosesOn, string OwnerKey, DateOnly On);

public static class StartInterestCheckEndpoint
{
    [WolverinePost("/checks")]
    public static (CreationResponse, IStartStream) Post(StartInterestCheck cmd)
    {
        var started = Decide(cmd);
        return (new CreationResponse($"/checks/{cmd.CheckId}/pitch"),
            MartenOps.StartStream<InterestCheck>(cmd.CheckId, started));
    }

    public static InterestCheckStarted Decide(StartInterestCheck cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.Title)) throw new Refusal("What's the idea called?");
        if (cmd.Goal < 1) throw new Refusal("How many people would make it worth doing? At least 1.");
        if (cmd.ClosesOn is { } closesOn && closesOn < cmd.On) throw new Refusal("The closing date has already passed");
        if (string.IsNullOrWhiteSpace(cmd.OwnerKey)) throw new Refusal("An owner key is needed");

        return new InterestCheckStarted(cmd.CheckId, cmd.Title.Trim(), cmd.Pitch?.Trim() ?? "", cmd.Goal, cmd.ClosesOn,
            OwnerKey.Hash(cmd.OwnerKey));
    }
}
