using System.Security.Cryptography;
using System.Text;

namespace CountMeIn;

/// <summary>A business rule said no. The message is what the person sees; Status is the HTTP code.</summary>
public class Refusal(string message, int status = 400) : Exception(message)
{
    public int Status { get; } = status;
}

public static class OwnerKey
{
    public static string Hash(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();

    public static bool Matches(string? sentKey, string storedHash) =>
        !string.IsNullOrEmpty(sentKey) &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Hash(sentKey)), Encoding.UTF8.GetBytes(storedHash));
}
