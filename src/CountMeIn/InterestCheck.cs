namespace CountMeIn;

/// <summary>The write model: just enough state to decide the next command.</summary>
public class InterestCheck
{
    public string Id { get; set; } = "";
    public string OwnerKeyHash { get; set; } = "";
    public bool Closed { get; set; }
    public int? Capacity { get; set; }
    public DateTimeOffset? DoorsOpen { get; set; }
    public DateTimeOffset? DoorsClose { get; set; }
    public HashSet<string> CheckedIn { get; set; } = [];
    /// <summary>Everyone counted in, in sign-up order. The first Capacity of them hold a spot; the rest are waitlisted.</summary>
    public List<string> PeopleIn { get; set; } = [];

    /// <summary>Everyone who ever used Change My Mind, even if they counted back in.</summary>
    public HashSet<string> ChangedMind { get; set; } = [];
    public string? Venue { get; set; }
    public DateTimeOffset? RevealAt { get; set; }
    public bool Released { get; set; }
    public HashSet<string> LostTheirSpot { get; set; } = [];
    public HashSet<string> LetIn { get; set; } = [];
    /// <summary>Everyone who turned up without a spot, in the order they first tried.</summary>
    public List<string> TriedAtDoor { get; set; } = [];

    public static InterestCheck Create(InterestCheckStarted e) =>
        new() { Id = e.CheckId, OwnerKeyHash = e.OwnerKeyHash, Capacity = e.Capacity, DoorsOpen = e.DoorsOpen, DoorsClose = e.DoorsClose };
    public void Apply(InterestShown e) => PeopleIn.Add(e.PersonId);
    public void Apply(InterestWithdrawn e) { PeopleIn.Remove(e.PersonId); CheckedIn.Remove(e.PersonId); ChangedMind.Add(e.PersonId); }
    public void Apply(CheckedIn e) => CheckedIn.Add(e.PersonId);

    public void Apply(VenueSet e) { Venue = e.Venue; RevealAt = e.RevealAt; }

    /// <summary>Counted in, and holding a spot or let in from the waitlist.</summary>
    public bool HoldsSpot(string personId) =>
        PeopleIn.Contains(personId) && !LostTheirSpot.Contains(personId) && !IsWaitlisted(personId);

    public void Apply(TurnedAwayAtDoor e) { if (!TriedAtDoor.Contains(e.PersonId)) TriedAtDoor.Add(e.PersonId); }
    public void Apply(UnclaimedSpotsReleased e)
    {
        Released = true;
        LostTheirSpot.UnionWith(e.LostTheirSpot);
        LetIn.UnionWith(e.LetIn);
    }

    /// <summary>Ten minutes before the doors close, or when they open if the window is shorter than that.</summary>
    public DateTimeOffset? ReleaseAt => DoorsOpen is { } open && DoorsClose is { } close ? ReleaseAtFor(open, close) : null;
    public static DateTimeOffset ReleaseAtFor(DateTimeOffset open, DateTimeOffset close) =>
        close.AddMinutes(-10) > open ? close.AddMinutes(-10) : open;

    /// <summary>People who neither lost their spot nor were let in keep their place in line; lost spots stay empty unless refilled.</summary>
    public bool IsWaitlisted(string personId) =>
        Capacity is { } c && !LetIn.Contains(personId) && !LostTheirSpot.Contains(personId) &&
        PeopleIn.Where(p => !LostTheirSpot.Contains(p) && !LetIn.Contains(p)).ToList().IndexOf(personId) >= c - LostTheirSpot.Count;
    public bool DoorsAreOpen(DateTimeOffset at) => DoorsOpen <= at && at <= DoorsClose;

    public void Apply(InterestCheckClosed e) => Closed = true;
}
