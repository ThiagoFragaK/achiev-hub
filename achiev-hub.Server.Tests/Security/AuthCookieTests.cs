using achiev_hub.Server.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;

namespace achiev_hub.Server.Tests.Security;

public class AuthCookieTests
{
    [Fact]
    public void Append_sets_httpOnly_cookie_and_Read_returns_token()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        AuthCookie.Append(context.Response, context.Request, "jwt-token-value", 30);

        var setCookie = context.Response.Headers.SetCookie.ToString();
        Assert.Contains(AuthCookie.HostPrefixedName, setCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);

        // Simulate subsequent request with the cookie.
        context.Request.Headers.Cookie = $"{AuthCookie.HostPrefixedName}=jwt-token-value";
        Assert.Equal("jwt-token-value", AuthCookie.Read(context.Request));
    }

    [Fact]
    public void Delete_clears_auth_cookies()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        AuthCookie.Append(context.Response, context.Request, "x", 30);
        AuthCookie.Delete(context.Response, context.Request);

        var setCookie = context.Response.Headers.SetCookie.ToString();
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
    }
}
