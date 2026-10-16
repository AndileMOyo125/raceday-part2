using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Helpers;

/// <summary>
/// Checks the session before an action runs.
/// No session user gives 401 Unauthorized. A logged in user with the wrong role gives 403 Forbidden.
/// Usage: [SessionAuthorize] for any logged in user, [SessionAuthorize(Roles = RoleNames.Organiser)] for one role.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>Comma separated list of allowed roles. Leave empty to allow any logged in user.</summary>
    public string? Roles { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetUserId();

        if (userId == null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "You must be logged in to use this endpoint." });
            return;
        }

        if (!string.IsNullOrWhiteSpace(Roles))
        {
            var allowedRoles = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var role = session.GetRole();

            if (role == null || !allowedRoles.Contains(role))
            {
                context.Result = new ObjectResult(new { message = $"This endpoint is only available to: {Roles}." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
