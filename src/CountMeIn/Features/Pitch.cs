using Wolverine.Http;
using Wolverine.Http.Marten;

namespace CountMeIn.Features;

/// <summary>What anyone with the link sees. No names, no emails, no owner key.</summary>
public class Pitch
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int Goal { get; set; }
    public int Count { get; set; }
    public bool GoalReached => Count >= Goal;
    public bool Open { get; set; } = true;
    public DateOnly? ClosesOn { get; set; }

    public static Pitch Create(InterestCheckStarted e) =>
        new() { Id = e.CheckId, Title = e.Title, Description = e.Pitch, Goal = e.Goal, ClosesOn = e.ClosesOn };
    public void Apply(InterestShown e) => Count++;
    public void Apply(InterestWithdrawn e) => Count--;
    public void Apply(InterestCheckClosed e) => Open = false;
}

public static class PitchEndpoint
{
    [WolverineGet("/checks/{checkId}/pitch")]
    public static Pitch Get([Document("checkId")] Pitch pitch) => pitch;
}
