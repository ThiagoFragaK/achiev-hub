namespace achiev_hub.Server.Infrastructure.Security;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Apply(context);
        await _next(context);
    }

    internal void Apply(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-Frame-Options"] = "DENY";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        if (context.User.Identity?.IsAuthenticated == true)
        {
            headers["Cache-Control"] = "no-store";
        }

        if (!_env.IsDevelopment())
        {
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "img-src 'self' data: https://media.steampowered.com https://shared.akamai.steamstatic.com; " +
                "style-src 'self'; " +
                "script-src 'self'; " +
                "object-src 'none'; " +
                "base-uri 'self'; " +
                "frame-ancestors 'none'";
        }
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
