using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Register, log in and log out.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : RaceDayControllerBase
{
    private readonly RaceDayContext _db;

    public AuthController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Creates a new account as an Organiser or a Participant.</summary>
    /// <remarks>
    /// Body: fullName, email, password (min 6 characters) and role ("Organiser" or "Participant").
    /// The password is hashed before it is saved.
    /// Returns 201 with the new user, or 400 if a field is invalid, the role is unknown or the email is already used.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == request.Role);
        if (role == null)
        {
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return BadRequest(new { message = "That email address is already registered." });
        }

        var user = new User
        {
            RoleId = role.RoleId,
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHelper.Hash(request.Password),
            CreatedDate = DateTime.Now
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var response = new UserResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = role.RoleName,
            CreatedDate = user.CreatedDate
        };

        return Created("/api/users/me", response);
    }

    /// <summary>Logs a user in and starts a session.</summary>
    /// <remarks>
    /// Body: email and password.
    /// Returns 200 with the user (the session cookie is set so later requests are authenticated),
    /// or 401 if the email or password is wrong.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !PasswordHelper.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // start a fresh session and store who the user is and their role
        HttpContext.Session.Clear();
        HttpContext.Session.SignIn(user.UserId, user.Role.RoleName);

        return Ok(new UserResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.RoleName,
            CreatedDate = user.CreatedDate
        });
    }

    /// <summary>Logs the current user out by clearing the session.</summary>
    /// <remarks>No body. Always returns 200.</remarks>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}
