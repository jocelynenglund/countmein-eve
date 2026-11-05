namespace CountMeIn;

/// <summary>The write model: just enough state to decide the next command.</summary>
public class InterestCheck
{
    public string Id { get; set; } = "";
    public string OwnerKeyHash { get; set; } = "";
    public bool Closed { get; set; }
    public int? Capacity { get; set; }
    /// <summary>Everyone counted in, in sign-up order. The first Capacity of them hold a spot; the rest are waitlisted.</summary>
    public List<string> PeopleIn { get; set; } = [];

    public static InterestCheck Create(InterestCheckStarted e) =>
        new() { Id = e.CheckId, OwnerKeyHash = e.OwnerKeyHash, Capacity = e.Capacity };
    public void Apply(InterestShown e) => PeopleIn.Add(e.PersonId);
    public void Apply(InterestWithdrawn e) => PeopleIn.Remove(e.PersonId);
    public void Apply(InterestCheckClosed e) => Closed = true;
}
