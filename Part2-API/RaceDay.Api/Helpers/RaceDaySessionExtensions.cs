using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Helpers;

/// <summary>
/// Small helpers so the controllers don't need to repeat the session key names.
/// </summary>
public static class RaceDaySessionExtensions
{
    private const string UserIdKey = "UserId";
    private const string RoleKey = "Role";

    public static void SignIn(this ISession session, int userId, string role)
    {
        session.SetInt32(UserIdKey, userId);
        session.SetString(RoleKey, role);
    }

    public static int? GetUserId(this ISession session) => session.GetInt32(UserIdKey);

    public static string? GetRole(this ISession session) => session.GetString(RoleKey);
}
