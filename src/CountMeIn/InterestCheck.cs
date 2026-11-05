namespace CountMeIn;

/// <summary>The write model: just enough state to decide the next command.</summary>
public class InterestCheck
{
    public string Id { get; set; } = "";
    public string OwnerKeyHash { get; set; } = "";
    public bool Closed { get; set; }
    public HashSet<string> PeopleIn { get; set; } = [];

    public static InterestCheck Create(InterestCheckStarted e) => new() { Id = e.CheckId, OwnerKeyHash = e.OwnerKeyHash };
    public void Apply(InterestShown e) => PeopleIn.Add(e.PersonId);
    public void Apply(InterestWithdrawn e) => PeopleIn.Remove(e.PersonId);
    public void Apply(InterestCheckClosed e) => Closed = true;
}
