using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Participants entering events, and organisers viewing who entered.</summary>
[ApiController]
[Route("api/events/{eventId:int}/enrolments")]
[SessionAuthorize]
public class EnrolmentsController : RaceDayControllerBase
{
    private readonly RaceDayContext _db;

    public EnrolmentsController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Lets a Participant enter an event under a chosen category.</summary>
    /// <remarks>
    /// Body: categoryId (must be a category of this event).
    /// Links the participant, the event and the category.
    /// Returns 201 with the enrolment, 400 if the category belongs to a different event,
    /// 401 if not logged in, 403 if not a Participant, 404 if the event or category doesn't exist,
    /// or 409 if the participant is already enrolled in this event.
    /// </remarks>
    [HttpPost]
    [SessionAuthorize(Roles = RoleNames.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int eventId, EnrolRequest request)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFoundMessage("Event not found.");
        }

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);
        if (category == null)
        {
            return NotFoundMessage("Category not found.");
        }

        if (category.EventId != eventId)
        {
            return BadRequest(new { message = "That category does not belong to this event." });
        }

        if (await _db.EventEnrolments.AnyAsync(x => x.EventId == eventId && x.ParticipantId == CurrentUserId))
        {
            return Conflict(new { message = "You are already enrolled in this event." });
        }

        var enrolment = new EventEnrolment
        {
            ParticipantId = CurrentUserId,
            EventId = eventId,
            CategoryId = request.CategoryId,
            EnrolmentDate = DateTime.Now
        };

        _db.EventEnrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var response = await _db.EventEnrolments
            .Where(x => x.EnrolmentId == enrolment.EnrolmentId)
            .Select(Projections.ToEnrolment)
            .FirstAsync();

        return Created($"/api/events/{eventId}/enrolments", response);
    }

    /// <summary>Lets an Organiser view everyone who has enrolled in their event.</summary>
    /// <remarks>
    /// No body. Returns 200 with a list of enrolments, 401 if not logged in,
    /// 403 if not an Organiser or not the owner of the event, or 404 if the event doesn't exist.
    /// </remarks>
    [HttpGet]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
        {
            return NotFoundMessage("Event not found.");
        }

        if (ev.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only view enrolments for events that you created.");
        }

        var enrolments = await _db.EventEnrolments
            .Where(x => x.EventId == eventId)
            .OrderBy(x => x.EnrolmentDate)
            .Select(Projections.ToEnrolment)
            .ToListAsync();

        return Ok(enrolments);
    }
}
