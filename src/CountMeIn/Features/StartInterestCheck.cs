using Marten.Events;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

namespace CountMeIn.Features;

public record StartInterestCheck(string CheckId, string Title, string? Pitch, int Goal, DateOnly? ClosesOn, string OwnerKey, DateOnly On,
    int? Capacity = null, DateTimeOffset? DoorsOpen = null, DateTimeOffset? DoorsClose = null);

public static class StartInterestCheckEndpoint
{
    [WolverinePost("/checks")]
    public static (CreationResponse, IStartStream, OutgoingMessages) Post(StartInterestCheck cmd)
    {
        var started = Decide(cmd);

        // The release happens by itself: a durable scheduled message, due when the last ten minutes begin.
        var later = new OutgoingMessages();
        if (started is { Capacity: not null, DoorsOpen: { } open, DoorsClose: { } close })
            later.Add(new ReleaseUnclaimedSpots(cmd.CheckId).ScheduledAt(InterestCheck.ReleaseAtFor(open, close)));

        return (new CreationResponse($"/checks/{cmd.CheckId}/pitch"),
            MartenOps.StartStream<InterestCheck>(cmd.CheckId, started), later);
    }

    public static InterestCheckStarted Decide(StartInterestCheck cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.Title)) throw new Refusal("What's the idea called?");
        if (cmd.Goal < 1) throw new Refusal("How many people would make it worth doing? At least 1.");
        if (cmd.ClosesOn is { } closesOn && closesOn < cmd.On) throw new Refusal("The closing date has already passed");
        if (cmd.Capacity < 1) throw new Refusal("Capacity must be at least 1");
        if (cmd.DoorsOpen.HasValue != cmd.DoorsClose.HasValue) throw new Refusal("The entry window needs both doorsOpen and doorsClose");
        if (cmd.DoorsOpen >= cmd.DoorsClose) throw new Refusal("The doors must open before they close");
        if (string.IsNullOrWhiteSpace(cmd.OwnerKey)) throw new Refusal("An owner key is needed");

        return new InterestCheckStarted(cmd.CheckId, cmd.Title.Trim(), cmd.Pitch?.Trim() ?? "", cmd.Goal, cmd.ClosesOn,
            OwnerKey.Hash(cmd.OwnerKey),
            cmd.Capacity, cmd.DoorsOpen, cmd.DoorsClose);
    }
}
