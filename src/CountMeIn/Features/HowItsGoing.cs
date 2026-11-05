using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;
using Wolverine.Http.Marten;

namespace CountMeIn.Features;

public record DayTally(DateOnly Day, int In, int Out);
public record Person(string PersonId, string Name);

/// <summary>The owner's view. Holds the owner key hash, so it is never returned as-is.</summary>
public class HowItsGoing
{
    public string Id { get; set; } = "";
    public string OwnerKeyHash { get; set; } = "";
    public int Goal { get; set; }
    public List<DayTally> ByDay { get; set; } = [];
    public List<Person> People { get; set; } = [];
    public int Count => People.Count;
    public bool GoalReached => Count >= Goal;

    public static HowItsGoing Create(InterestCheckStarted e) => new() { Id = e.CheckId, Goal = e.Goal, OwnerKeyHash = e.OwnerKeyHash };

    public void Apply(InterestShown e)
    {
        People.Add(new Person(e.PersonId, e.Name));
        Tally(e.On, 1, 0);
    }

    public void Apply(InterestWithdrawn e)
    {
        People.RemoveAll(p => p.PersonId == e.PersonId);
        Tally(e.On, 0, 1);
    }

    private void Tally(DateOnly day, int @in, int @out)
    {
        var existing = ByDay.FirstOrDefault(d => d.Day == day);
        if (existing is not null) ByDay.Remove(existing);
        ByDay.Add(new DayTally(day, (existing?.In ?? 0) + @in, (existing?.Out ?? 0) + @out));
        ByDay.Sort((a, b) => a.Day.CompareTo(b.Day));
    }
}

public record HowItsGoingResponse(int Count, int Goal, bool GoalReached, IReadOnlyList<DayTally> ByDay, IReadOnlyList<string> People);

public static class HowItsGoingEndpoint
{
    [WolverineGet("/checks/{checkId}/how-its-going")]
    public static HowItsGoingResponse Get([Document("checkId")] HowItsGoing view, [FromHeader(Name = "X-Owner-Key")] string? ownerKey)
    {
        if (!OwnerKey.Matches(ownerKey, view.OwnerKeyHash)) throw new Refusal("Only the owner can see this", 403);
        return new HowItsGoingResponse(view.Count, view.Goal, view.GoalReached, view.ByDay, view.People.Select(p => p.Name).ToList());
    }
}
