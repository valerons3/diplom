using WebBackend.Repositories.Interfaces;

namespace WebBackend.Middleware;

public class RevokedTokenMiddleware
{
    private readonly RequestDelegate next;

    public RevokedTokenMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRevokedTokenRepository revokedTokenRepo)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            await next(context);
            return;
        }
        
        var token = authHeader.ToString()
            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        
        if (string.IsNullOrEmpty(token))
        {
            await next(context);
            return;
        }
        
        bool isRevoked = await revokedTokenRepo.IsTokenRevokedAsync(token);
        
        if (isRevoked)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Токен был отозван");
            return;
        }
        
        await next(context);
    }
}

public static class RevokedTokenMiddlewareExtensions
{
    public static IApplicationBuilder UseRevokedTokenMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RevokedTokenMiddleware>();
    }
}