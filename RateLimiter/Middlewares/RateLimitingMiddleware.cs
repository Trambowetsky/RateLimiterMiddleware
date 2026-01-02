using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace RateLimiter.Middlewares;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitingOptions _options;
    
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requests
        = new();

    public RateLimitingMiddleware(RequestDelegate next, IOptions<RateLimitingOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        var queue = _requests.GetOrAdd(key, _ => new ConcurrentQueue<DateTime>());

        while (queue.TryPeek(out var time) && time < now - TimeSpan.FromSeconds(_options.WindowSeconds))
        {
            queue.TryDequeue(out _);
        }
        if (queue.Count >= _options.MaxRequests)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsync("Too many requests");
            return;
        }
        queue.Enqueue(now);

        await _next(context);
    }
}