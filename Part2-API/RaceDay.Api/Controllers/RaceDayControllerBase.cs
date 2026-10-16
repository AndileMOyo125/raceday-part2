using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Controllers;

/// <summary>Shared helpers for all the controllers.</summary>
public abstract class RaceDayControllerBase : ControllerBase
{
    /// <summary>The id of the user stored in the session (0 if nobody is logged in).</summary>
    protected int CurrentUserId => HttpContext.Session.GetUserId() ?? 0;

    protected ObjectResult Forbidden(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, new { message });

    protected NotFoundObjectResult NotFoundMessage(string message) =>
        NotFound(new { message });
}
