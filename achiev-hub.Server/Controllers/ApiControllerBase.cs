using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using achiev_hub.Server.Exceptions;
using achiev_hub.Server.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult HandleException(Exception exception)
    {
        return exception switch
        {
            NotFoundException => NotFound(new { message = exception.Message }),
            ConflictException => Conflict(new { message = exception.Message }),
            SteamApiException => StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = SteamApiException.UserMessage }),
            _ => throw exception
        };
    }

    protected bool IsGuest()
    {
        return User.IsInRole(JwtTokenService.GuestRole)
            || User.FindFirstValue(JwtTokenService.TokenKindClaim) == JwtTokenService.GuestTokenKind;
    }

    protected int? ResolveUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) && userId > 0 ? userId : null;
    }

    protected int? ResolveUserIdForDbReads()
    {
        return IsGuest() ? null : ResolveUserId();
    }
}
