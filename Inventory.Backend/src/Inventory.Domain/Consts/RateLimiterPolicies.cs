namespace Inventory.Domain.Consts;

public static class RateLimiterPolicies
{
    public const string FixedWindow = "FixedWindow";
    public const string TokenBucket = "TokenBucket";
}