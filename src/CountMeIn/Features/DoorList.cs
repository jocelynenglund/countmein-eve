using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;
using Wolverine.Http.Marten;

namespace CountMeIn.Features;

public record Arrival(string Name, DateTimeOffset At);

/// <summary>The owner's door list. Holds the owner key hash, so it is never returned as-is.</summary>
public class DoorList
{
    public string Id { get; set; } = "";
    public string OwnerKeyHash { get; set; } = "";
    public int? Capacity { get; set; }
    public List<Person> People { get; set; } = [];
    public List<string> Arrived { get; set; } = [];
    public List<Arrival> CheckedIn { get; set; } = [];
    public List<Arrival> TurnedAway { get; set; } = [];

    public IEnumerable<Person> SpotHolders => People.Take(Capacity ?? int.MaxValue);
    public List<string> NotArrivedYet => SpotHolders.Where(p => !Arrived.Contains(p.PersonId)).Select(p => p.Name).ToList();
    public List<string> Waitlist => People.Skip(Capacity ?? int.MaxValue).Select(p => p.Name).ToList();

    public static DoorList Create(InterestCheckStarted e) => new() { Id = e.CheckId, OwnerKeyHash = e.OwnerKeyHash, Capacity = e.Capacity };

    public void Apply(InterestShown e) => People.Add(new Person(e.PersonId, e.Name));

    public void Apply(InterestWithdrawn e)
    {
        var name = People.FirstOrDefault(p => p.PersonId == e.PersonId)?.Name;
        People.RemoveAll(p => p.PersonId == e.PersonId);
        if (Arrived.Remove(e.PersonId)) CheckedIn.RemoveAll(a => a.Name == name);
    }

    public void Apply(CheckedIn e)
    {
        var person = People.First(p => p.PersonId == e.PersonId);
        Arrived.Add(person.PersonId);
        CheckedIn.Add(new Arrival(person.Name, e.At));
    }

    public void Apply(TurnedAwayAtDoor e) =>
        TurnedAway.Add(new Arrival(People.First(p => p.PersonId == e.PersonId).Name, e.At));
}

public record DoorListResponse(IReadOnlyList<Arrival> CheckedIn, IReadOnlyList<string> NotArrivedYet,
    IReadOnlyList<string> Waitlist, IReadOnlyList<Arrival> TurnedAway);

public static class DoorListEndpoint
{
    [WolverineGet("/checks/{checkId}/door-list")]
    public static DoorListResponse Get([Document("checkId")] DoorList view, [FromHeader(Name = "X-Owner-Key")] string? ownerKey)
    {
        if (!OwnerKey.Matches(ownerKey, view.OwnerKeyHash)) throw new Refusal("Only the owner can see this", 403);
        return new DoorListResponse(view.CheckedIn, view.NotArrivedYet, view.Waitlist, view.TurnedAway);
    }
}
