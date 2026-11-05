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

    public string? Venue { get; set; }
    public DateTimeOffset? RevealAt { get; set; }
    public List<string> LostTheirSpotIds { get; set; } = [];
    public List<string> LetInIds { get; set; } = [];
    public List<string> LostTheirSpot { get; set; } = [];
    public List<string> LetInFromWaitlist { get; set; } = [];

    // Spots lost at the release stay empty; the people let in hold the others' places in line.
    List<Person> InLine => People.Where(p => !LostTheirSpotIds.Contains(p.PersonId) && !LetInIds.Contains(p.PersonId)).ToList();
    int InLineSlots => (Capacity ?? int.MaxValue) - LostTheirSpotIds.Count;
    public List<string> NotArrivedYet => People.Where(p => LetInIds.Contains(p.PersonId))
        .Concat(InLine.Take(InLineSlots)).Where(p => !Arrived.Contains(p.PersonId)).Select(p => p.Name).ToList();
    public List<string> Waitlist => InLine.Skip(InLineSlots).Select(p => p.Name).ToList();

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

    public void Apply(VenueSet e) { Venue = e.Venue; RevealAt = e.RevealAt; }

    public void Apply(UnclaimedSpotsReleased e)
    {
        LostTheirSpotIds.AddRange(e.LostTheirSpot);
        LetInIds.AddRange(e.LetIn);
        LostTheirSpot.AddRange(NamesOf(e.LostTheirSpot));
        LetInFromWaitlist.AddRange(NamesOf(e.LetIn));
    }

    IEnumerable<string> NamesOf(IEnumerable<string> ids) => ids.Select(id => People.First(p => p.PersonId == id).Name);

    public void Apply(TurnedAwayAtDoor e) =>
        TurnedAway.Add(new Arrival(People.First(p => p.PersonId == e.PersonId).Name, e.At));
}

public record DoorListResponse(IReadOnlyList<Arrival> CheckedIn, IReadOnlyList<string> NotArrivedYet,
    IReadOnlyList<string> Waitlist, IReadOnlyList<Arrival> TurnedAway,
    IReadOnlyList<string> LetInFromWaitlist, IReadOnlyList<string> LostTheirSpot,
    string? Venue, DateTimeOffset? RevealAt);

public static class DoorListEndpoint
{
    [WolverineGet("/checks/{checkId}/door-list")]
    public static DoorListResponse Get([Document("checkId")] DoorList view, [FromHeader(Name = "X-Owner-Key")] string? ownerKey)
    {
        if (!OwnerKey.Matches(ownerKey, view.OwnerKeyHash)) throw new Refusal("Only the owner can see this", 403);
        return new DoorListResponse(view.CheckedIn, view.NotArrivedYet, view.Waitlist, view.TurnedAway,
            view.LetInFromWaitlist, view.LostTheirSpot, view.Venue, view.RevealAt);
    }
}
