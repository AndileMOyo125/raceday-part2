using System.Linq.Expressions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>
/// Reusable EF queries that turn database entities into the response objects.
/// Keeping them in one place means every endpoint returns the same shape.
/// </summary>
public static class Projections
{
    public static readonly Expression<Func<User, UserResponse>> ToUser = u => new UserResponse
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role.RoleName,
        CreatedDate = u.CreatedDate
    };

    public static readonly Expression<Func<Event, EventResponse>> ToEvent = e => new EventResponse
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.FullName,
        EventName = e.EventName,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType
    };

    public static readonly Expression<Func<Category, CategoryResponse>> ToCategory = c => new CategoryResponse
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        MinAge = c.MinAge,
        MaxAge = c.MaxAge
    };

    public static readonly Expression<Func<EventEnrolment, EnrolmentResponse>> ToEnrolment = x => new EnrolmentResponse
    {
        EnrolmentId = x.EnrolmentId,
        ParticipantId = x.ParticipantId,
        ParticipantName = x.Participant.FullName,
        EventId = x.EventId,
        EventName = x.Event.EventName,
        CategoryId = x.CategoryId,
        CategoryName = x.Category.CategoryName,
        EnrolmentDate = x.EnrolmentDate
    };

    public static readonly Expression<Func<Result, ResultResponse>> ToResult = r => new ResultResponse
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        EventId = r.Enrolment.EventId,
        EventName = r.Enrolment.Event.EventName,
        ParticipantId = r.Enrolment.ParticipantId,
        ParticipantName = r.Enrolment.Participant.FullName,
        CategoryName = r.Enrolment.Category.CategoryName,
        FinishTime = r.FinishTime,
        FinishPosition = r.FinishPosition
    };
}
