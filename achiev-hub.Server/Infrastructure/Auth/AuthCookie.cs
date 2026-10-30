using Microsoft.Extensions.Options;

namespace achiev_hub.Server.Infrastructure.Auth;

public static class AuthCookie
{
    public const string Name = "achiev_hub_at";
    public const string HostPrefixedName = "__Host-achiev_hub_at";

    public static string ResolveName(HttpRequest request) =>
        request.IsHttps ? HostPrefixedName : Name;

    public static void Append(HttpResponse response, HttpRequest request, string token, int expiryMinutes)
    {
        var name = ResolveName(request);
        response.Cookies.Append(name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(Math.Max(1, expiryMinutes))
        });

        // Clear the alternate name so HTTP/HTTPS switches do not leave stale cookies.
        var other = name == Name ? HostPrefixedName : Name;
        response.Cookies.Delete(other, new CookieOptions { Path = "/" });
    }

    public static void Delete(HttpResponse response, HttpRequest request)
    {
        response.Cookies.Delete(Name, new CookieOptions { Path = "/" });
        response.Cookies.Delete(HostPrefixedName, new CookieOptions { Path = "/", Secure = true });
    }

    public static string? Read(HttpRequest request)
    {
        if (request.Cookies.TryGetValue(HostPrefixedName, out var hostToken) && !string.IsNullOrWhiteSpace(hostToken))
        {
            return hostToken;
        }

        if (request.Cookies.TryGetValue(Name, out var token) && !string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        return null;
    }
}
