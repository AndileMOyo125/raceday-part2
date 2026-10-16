using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Controllers;

/// <summary>The logged in user's own profile, enrolments and results.</summary>
[ApiController]
[Route("api/users/me")]
[SessionAuthorize]
public class UsersController : RaceDayControllerBase
{
    private readonly RaceDayContext _db;

    public UsersController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Returns the profile of the logged in user (Organiser or Participant).</summary>
    /// <remarks>No body. Returns 200 with the profile, or 401 if not logged in.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _db.Users
            .Where(u => u.UserId == CurrentUserId)
            .Select(Projections.ToUser)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return Unauthorized(new { message = "Your account could not be found. Please log in again." });
        }

        return Ok(user);
    }

    /// <summary>Updates the logged in user's own name and email.</summary>
    /// <remarks>
    /// Body: fullName and email.
    /// Returns 200 with the updated profile, 400 if the data is invalid or the email belongs to someone else,
    /// or 401 if not logged in.
    /// </remarks>
    [HttpPut]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user == null)
        {
            return Unauthorized(new { message = "Your account could not be found. Please log in again." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != user.UserId))
        {
            return BadRequest(new { message = "That email address is already used by another account." });
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();

        var updated = await _db.Users
            .Where(u => u.UserId == user.UserId)
            .Select(Projections.ToUser)
            .FirstAsync();

        return Ok(updated);
    }

    /// <summary>Lets a Participant view their own enrolment history.</summary>
    /// <remarks>No body. Returns 200 with a list of enrolments, 401 if not logged in or 403 if not a Participant.</remarks>
    [HttpGet("enrolments")]
    [SessionAuthorize(Roles = RoleNames.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var enrolments = await _db.EventEnrolments
            .Where(x => x.ParticipantId == CurrentUserId)
            .OrderByDescending(x => x.EnrolmentDate)
            .Select(Projections.ToEnrolment)
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Lets a Participant view only their own results across all events.</summary>
    /// <remarks>No body. Returns 200 with a list of results, 401 if not logged in or 403 if not a Participant.</remarks>
    [HttpGet("results")]
    [SessionAuthorize(Roles = RoleNames.Participant)]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyResults()
    {
        var results = await _db.Results
            .Where(r => r.Enrolment.ParticipantId == CurrentUserId)
            .Select(Projections.ToResult)
            .ToListAsync();

        return Ok(results);
    }
}
