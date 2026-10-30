using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Common.Interfaces;
using achiev_hub.Server.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult HandleException(Exception exception)
    {
        return exception switch
        {
            NotFoundException => NotFound(new { message = exception.Message }),
            ConflictException => Conflict(new { message = exception.Message }),
            BadRequestException => BadRequest(new { message = exception.Message }),
            SteamApiException => StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = SteamApiException.UserMessage }),
            _ => throw exception
        };
    }

    protected bool IsGuest() => AccessControl.IsGuest(User);

    protected bool IsAdmin() => AccessControl.IsAdmin(User);

    protected int? ResolveUserId() => AccessControl.TryGetUserId(User);

    protected int? ResolveUserIdForDbReads()
    {
        return IsGuest() ? null : ResolveUserId();
    }

    protected ActionResult? ForbidUnlessAdmin()
    {
        return IsAdmin() ? null : Forbid();
    }

    protected ActionResult? ForbidUnlessSelfOrAdmin(int resourceUserId)
    {
        return AccessControl.CanAccessOwnedResource(User, resourceUserId) ? null : NotFound();
    }

    protected int RequireAuthenticatedUserId()
    {
        return ResolveUserId()
            ?? throw new UnauthorizedAccessException("Authenticated user id is required.");
    }

    protected ActionResult? ForbidGuests(string message = "Guests cannot use this endpoint.")
    {
        return IsGuest() ? StatusCode(StatusCodes.Status403Forbidden, new { message }) : null;
    }

    protected ActionResult? TryResolveBoundSteamId(string? requestedSteamId, out string steamId)
    {
        steamId = string.Empty;
        if (!SteamIdBinder.TryResolve(AccessControl.TryGetSteamId(User), requestedSteamId, out steamId, out var error))
        {
            return BadRequest(new { message = error });
        }

        return null;
    }
}
