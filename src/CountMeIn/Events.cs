namespace CountMeIn;

public record InterestCheckStarted(string CheckId, string Title, string Pitch, int Goal, DateOnly? ClosesOn, string OwnerKeyHash,
    int? Capacity = null, DateTimeOffset? DoorsOpen = null, DateTimeOffset? DoorsClose = null);

public record InterestShown(string CheckId, string PersonId, string Name, string? Email, DateOnly On);

public record InterestWithdrawn(string CheckId, string PersonId, DateOnly On);

public record InterestCheckClosed(string CheckId, DateOnly On);

public record CheckedIn(string CheckId, string PersonId, DateTimeOffset At);

/// <summary>A waitlisted person showed up at the door. A fact the owner wants to see, even though the person was refused.</summary>
public record TurnedAwayAtDoor(string CheckId, string PersonId, DateTimeOffset At);
