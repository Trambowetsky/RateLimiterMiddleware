namespace RateLimiter;

public class RateLimitingOptions
{
    public int WindowSeconds { get; set; }
    public int MaxRequests { get; set; }
}