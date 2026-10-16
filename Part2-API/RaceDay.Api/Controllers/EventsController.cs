using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Create, view, update and delete events.</summary>
[ApiController]
[Route("api/events")]
[SessionAuthorize]
public class EventsController : RaceDayControllerBase
{
    private readonly RaceDayContext _db;

    public EventsController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Returns all events so users can browse them (both roles).</summary>
    /// <remarks>
    /// No body. Optional query string upcomingOnly=true only returns events that haven't happened yet.
    /// Returns 200 with a list of events, or 401 if not logged in.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEvents([FromQuery] bool upcomingOnly = false)
    {
        var query = _db.Events.AsQueryable();
        if (upcomingOnly)
        {
            query = query.Where(e => e.EventDate >= DateTime.Now);
        }

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(Projections.ToEvent)
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Returns the full detail of a single event.</summary>
    /// <remarks>No body. Returns 200 with the event, 404 if it doesn't exist or 401 if not logged in.</remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _db.Events
            .Where(e => e.EventId == id)
            .Select(Projections.ToEvent)
            .FirstOrDefaultAsync();

        if (ev == null)
        {
            return NotFoundMessage("Event not found.");
        }

        return Ok(ev);
    }

    /// <summary>Allows an Organiser to create a new event.</summary>
    /// <remarks>
    /// Body: eventName, description, eventDate, location, distanceKm and eventType (Run, Walk or Cycle).
    /// Returns 201 with the new event, 400 if a field is invalid, 401 if not logged in or 403 if not an Organiser.
    /// </remarks>
    [HttpPost]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        var ev = new Event
        {
            OrganiserId = CurrentUserId,
            EventName = request.EventName.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = request.EventType
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var response = await _db.Events
            .Where(e => e.EventId == ev.EventId)
            .Select(Projections.ToEvent)
            .FirstAsync();

        return Created($"/api/events/{ev.EventId}", response);
    }

    /// <summary>Allows an Organiser to update an event they created.</summary>
    /// <remarks>
    /// Body: eventName, description, eventDate, location, distanceKm and eventType.
    /// Returns 200 with the updated event, 400 if invalid, 401 if not logged in,
    /// 403 if not an Organiser or not the owner of this event, or 404 if the event doesn't exist.
    /// </remarks>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
        {
            return NotFoundMessage("Event not found.");
        }

        if (ev.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only change events that you created.");
        }

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description?.Trim();
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = request.EventType;
        await _db.SaveChangesAsync();

        var response = await _db.Events
            .Where(e => e.EventId == id)
            .Select(Projections.ToEvent)
            .FirstAsync();

        return Ok(response);
    }

    /// <summary>Allows an Organiser to delete an event they created.</summary>
    /// <remarks>
    /// No body. Also removes the event's categories, enrolments and results.
    /// Returns 204 when deleted, 401 if not logged in, 403 if not an Organiser or not the owner, or 404 if it doesn't exist.
    /// </remarks>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
        {
            return NotFoundMessage("Event not found.");
        }

        if (ev.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only delete events that you created.");
        }

        // the foreign keys don't cascade, so remove the child rows first (results, enrolments, categories)
        var enrolments = await _db.EventEnrolments.Where(x => x.EventId == id).ToListAsync();
        var enrolmentIds = enrolments.Select(x => x.EnrolmentId).ToList();
        var results = await _db.Results.Where(r => enrolmentIds.Contains(r.EnrolmentId)).ToListAsync();
        var categories = await _db.Categories.Where(c => c.EventId == id).ToListAsync();

        _db.Results.RemoveRange(results);
        _db.EventEnrolments.RemoveRange(enrolments);
        _db.Categories.RemoveRange(categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
