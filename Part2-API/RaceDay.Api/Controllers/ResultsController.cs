using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Finish times and finishing positions.</summary>
[ApiController]
[Route("api")]
[SessionAuthorize]
public class ResultsController : RaceDayControllerBase
{
    private readonly RaceDayContext _db;

    public ResultsController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Lets an Organiser capture a finish time and position for a participant after the event.</summary>
    /// <remarks>
    /// Body: finishTime (hh:mm:ss) and finishPosition.
    /// Returns 201 with the result, 400 if invalid, 401 if not logged in,
    /// 403 if not an Organiser or not the owner of the event, 404 if the enrolment doesn't exist,
    /// or 409 if a result has already been captured for this enrolment.
    /// </remarks>
    [HttpPost("enrolments/{enrolmentId:int}/results")]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CaptureResult(int enrolmentId, ResultRequest request)
    {
        var enrolment = await _db.EventEnrolments
            .Include(x => x.Event)
            .FirstOrDefaultAsync(x => x.EnrolmentId == enrolmentId);

        if (enrolment == null)
        {
            return NotFoundMessage("Enrolment not found.");
        }

        if (enrolment.Event.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only capture results for events that you created.");
        }

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
        {
            return Conflict(new { message = "A result has already been captured for this enrolment." });
        }

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = request.FinishTime,
            FinishPosition = request.FinishPosition
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var response = await _db.Results
            .Where(r => r.ResultId == result.ResultId)
            .Select(Projections.ToResult)
            .FirstAsync();

        return Created($"/api/events/{enrolment.EventId}/results", response);
    }

    /// <summary>Returns the full result list for an event (both roles).</summary>
    /// <remarks>No body. Returns 200 with a list of results ordered by finishing position, 404 if the event doesn't exist or 401 if not logged in.</remarks>
    [HttpGet("events/{eventId:int}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFoundMessage("Event not found.");
        }

        var results = await _db.Results
            .Where(r => r.Enrolment.EventId == eventId)
            .OrderBy(r => r.FinishPosition)
            .Select(Projections.ToResult)
            .ToListAsync();

        return Ok(results);
    }
}
