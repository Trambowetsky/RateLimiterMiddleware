using System.Collections.Concurrent;

namespace RateLimiter.Middlewares;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly TimeSpan _window = TimeSpan.FromSeconds(10);
    private const int _maxRequests = 5;
    
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requests
        = new();

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        var queue = _requests.GetOrAdd(key, _ => new ConcurrentQueue<DateTime>());

        while (queue.TryPeek(out var time) && time < now - _window)
        {
            queue.TryDequeue(out _);
        }
        if (queue.Count >= _maxRequests)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsync("Too many requests");
            return;
        }
        queue.Enqueue(now);

        await _next(context);
    }
}