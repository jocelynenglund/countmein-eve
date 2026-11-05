using CountMeIn.Features;

namespace CountMeIn.Tests;

/// <summary>Shared Givens, using the values from the Count me in scenarios.</summary>
public static class Scenarios
{
    public static readonly DateOnly Nov4 = new(2026, 11, 4);

    public static InterestCheckStarted Started(int goal = 10, DateOnly? closesOn = null, string ownerKey = "k1",
        int? capacity = null, DateTimeOffset? doorsOpen = null, DateTimeOffset? doorsClose = null) =>
        new("c1", "Board games", "", goal, closesOn, OwnerKey.Hash(ownerKey), capacity, doorsOpen, doorsClose);

    public static InterestShown Shown(string personId, string name, DateOnly? on = null) =>
        new("c1", personId, name, null, on ?? Nov4);

    public static InterestWithdrawn Withdrawn(string personId, DateOnly? on = null) => new("c1", personId, on ?? Nov4);

    public static readonly InterestCheckClosed Closed = new("c1", Nov4);
}
