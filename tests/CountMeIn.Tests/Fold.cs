using System.Reflection;

namespace CountMeIn.Tests;

/// <summary>Folds events the way Marten does: static Create for the first event, instance Apply for the rest.</summary>
public static class Fold
{
    public static T? Of<T>(params object[] events) where T : class
    {
        if (events.Length == 0) return null;
        var state = (T)typeof(T).GetMethod("Create", BindingFlags.Public | BindingFlags.Static, [events[0].GetType()])!
            .Invoke(null, [events[0]])!;
        foreach (var e in events.Skip(1))
            typeof(T).GetMethod("Apply", [e.GetType()])?.Invoke(state, [e]);
        return state;
    }
}
