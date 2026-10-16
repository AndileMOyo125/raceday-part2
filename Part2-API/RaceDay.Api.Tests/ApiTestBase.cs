using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Tests;

/// <summary>Helper methods shared by the test classes.</summary>
public abstract class ApiTestBase
{
    protected readonly RaceDayFactory Factory;

    protected ApiTestBase(RaceDayFactory factory)
    {
        Factory = factory;
    }

    /// <summary>Registers a brand new user with a unique email and logs them in. The client keeps the session cookie.</summary>
    protected async Task<(HttpClient Client, UserResponse User)> CreateLoggedInUser(string role)
    {
        var client = Factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@raceday.test";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = $"Test {role}",
            email,
            password = "Password1!",
            role
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var user = await register.Content.ReadFromJsonAsync<UserResponse>();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        return (client, user!);
    }

    protected static object ValidEvent(string eventType = "Run") => new
    {
        eventName = "Test Fun Run",
        description = "A test event",
        eventDate = DateTime.Now.AddDays(30),
        location = "Johannesburg",
        distanceKm = 10,
        eventType
    };

    /// <summary>Organiser creates an event and one category for it.</summary>
    protected async Task<(EventResponse Event, CategoryResponse Category)> CreateEventWithCategory(HttpClient organiser)
    {
        var eventResponse = await organiser.PostAsJsonAsync("/api/events", ValidEvent());
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        var ev = (await eventResponse.Content.ReadFromJsonAsync<EventResponse>())!;

        var categoryResponse = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new
        {
            categoryName = "Senior",
            minAge = 20,
            maxAge = 99
        });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var category = (await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>())!;

        return (ev, category);
    }

    /// <summary>Participant enrols in an event and the enrolment is returned.</summary>
    protected async Task<EnrolmentResponse> Enrol(HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
    }
}
