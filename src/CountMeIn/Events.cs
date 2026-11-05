namespace CountMeIn;

public record InterestCheckStarted(string CheckId, string Title, string Pitch, int Goal, DateOnly? ClosesOn, string OwnerKeyHash,
    int? Capacity = null, DateTimeOffset? DoorsOpen = null, DateTimeOffset? DoorsClose = null);

public record InterestShown(string CheckId, string PersonId, string Name, string? Email, DateOnly On);

public record InterestWithdrawn(string CheckId, string PersonId, DateOnly On);

public record InterestCheckClosed(string CheckId, DateOnly On);
