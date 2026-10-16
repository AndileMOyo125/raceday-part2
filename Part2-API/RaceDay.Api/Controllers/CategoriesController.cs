using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Age and distance categories for events.</summary>
[ApiController]
[Route("api")]
[SessionAuthorize]
public class CategoriesController : RaceDayControllerBase
{
    private readonly RaceDayContext _db;

    public CategoriesController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Returns all categories for a specific event (both roles).</summary>
    /// <remarks>No body. Returns 200 with a list of categories, 404 if the event doesn't exist or 401 if not logged in.</remarks>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFoundMessage("Event not found.");
        }

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryName)
            .Select(Projections.ToCategory)
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Allows an Organiser to add an age or distance category to their event.</summary>
    /// <remarks>
    /// Body: categoryName, minAge and maxAge (ages are optional, e.g. for distance categories like 10km).
    /// Returns 201 with the new category, 400 if invalid, 401 if not logged in,
    /// 403 if not an Organiser or not the owner of the event, or 404 if the event doesn't exist.
    /// </remarks>
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
        {
            return NotFoundMessage("Event not found.");
        }

        if (ev.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only add categories to events that you created.");
        }

        if (request.MinAge.HasValue && request.MaxAge.HasValue && request.MinAge > request.MaxAge)
        {
            return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });
        }

        var category = new Category
        {
            EventId = eventId,
            CategoryName = request.CategoryName.Trim(),
            MinAge = request.MinAge,
            MaxAge = request.MaxAge
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var response = await _db.Categories
            .Where(c => c.CategoryId == category.CategoryId)
            .Select(Projections.ToCategory)
            .FirstAsync();

        return Created($"/api/events/{eventId}/categories", response);
    }

    /// <summary>Allows an Organiser to update a category on their own event.</summary>
    /// <remarks>
    /// Body: categoryName, minAge and maxAge.
    /// Returns 200 with the updated category, 400 if invalid, 401 if not logged in,
    /// 403 if not an Organiser or not the owner, or 404 if the category doesn't exist.
    /// </remarks>
    [HttpPut("categories/{id:int}")]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFoundMessage("Category not found.");
        }

        if (category.Event.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only change categories on events that you created.");
        }

        if (request.MinAge.HasValue && request.MaxAge.HasValue && request.MinAge > request.MaxAge)
        {
            return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });
        }

        category.CategoryName = request.CategoryName.Trim();
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        await _db.SaveChangesAsync();

        var response = await _db.Categories
            .Where(c => c.CategoryId == id)
            .Select(Projections.ToCategory)
            .FirstAsync();

        return Ok(response);
    }

    /// <summary>Allows an Organiser to remove a category from their own event.</summary>
    /// <remarks>
    /// No body. Returns 204 when deleted, 401 if not logged in, 403 if not an Organiser or not the owner,
    /// 404 if the category doesn't exist, or 409 if participants are already enrolled in the category.
    /// </remarks>
    [HttpDelete("categories/{id:int}")]
    [SessionAuthorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFoundMessage("Category not found.");
        }

        if (category.Event.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only delete categories on events that you created.");
        }

        if (await _db.EventEnrolments.AnyAsync(x => x.CategoryId == id))
        {
            return Conflict(new { message = "This category can't be deleted because participants are already enrolled in it." });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
