using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Tests;

/// <summary>Event and category management by Organisers.</summary>
public class EventTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public EventTests(RaceDayFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Organiser_CanCreateUpdateAndDeleteEvent()
    {
        var (organiser, user) = await CreateLoggedInUser("Organiser");

        // create
        var create = await organiser.PostAsJsonAsync("/api/events", ValidEvent());
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await create.Content.ReadFromJsonAsync<EventResponse>())!;
        Assert.Equal(user.UserId, created.OrganiserId);

        // update
        var update = await organiser.PutAsJsonAsync($"/api/events/{created.EventId}", new
        {
            eventName = "Renamed Event",
            description = "Updated description",
            eventDate = DateTime.Now.AddDays(60),
            location = "Pretoria",
            distanceKm = 21,
            eventType = "Cycle"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<EventResponse>())!;
        Assert.Equal("Renamed Event", updated.EventName);
        Assert.Equal("Cycle", updated.EventType);

        // delete
        var delete = await organiser.DeleteAsync($"/api/events/{created.EventId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        // gone
        var get = await organiser.GetAsync($"/api/events/{created.EventId}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewEvents()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, _) = await CreateEventWithCategory(organiser);
        var (participant, _) = await CreateLoggedInUser("Participant");

        var list = await participant.GetFromJsonAsync<List<EventResponse>>("/api/events");
        var single = await participant.GetAsync($"/api/events/{ev.EventId}");

        Assert.Contains(list!, e => e.EventId == ev.EventId);
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_Returns400()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", ValidEvent("Swim"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithMissingName_Returns400()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            description = "No name",
            eventDate = DateTime.Now.AddDays(10),
            location = "Durban",
            distanceKm = 5,
            eventType = "Walk"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ThatDoesNotExist_Returns404()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");

        var response = await organiser.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await CreateLoggedInUser("Organiser");
        var (ev, _) = await CreateEventWithCategory(owner);
        var (otherOrganiser, _) = await CreateLoggedInUser("Organiser");

        var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{ev.EventId}", ValidEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await CreateLoggedInUser("Organiser");
        var (ev, _) = await CreateEventWithCategory(owner);
        var (otherOrganiser, _) = await CreateLoggedInUser("Organiser");

        var response = await otherOrganiser.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ThatDoesNotExist_Returns404()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");

        var response = await organiser.PutAsJsonAsync("/api/events/999999", ValidEvent());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Categories_CanBeCreatedViewedUpdatedAndDeleted()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, category) = await CreateEventWithCategory(organiser);
        var (participant, _) = await CreateLoggedInUser("Participant");

        // both roles can view
        var list = await participant.GetFromJsonAsync<List<CategoryResponse>>($"/api/events/{ev.EventId}/categories");
        Assert.Contains(list!, c => c.CategoryId == category.CategoryId);

        // update
        var update = await organiser.PutAsJsonAsync($"/api/categories/{category.CategoryId}", new
        {
            categoryName = "Under 20",
            minAge = 0,
            maxAge = 19
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<CategoryResponse>())!;
        Assert.Equal("Under 20", updated.CategoryName);

        // delete
        var delete = await organiser.DeleteAsync($"/api/categories/{category.CategoryId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithMinAgeAboveMaxAge_Returns400()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, _) = await CreateEventWithCategory(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new
        {
            categoryName = "Impossible",
            minAge = 50,
            maxAge = 10
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_ForMissingEvent_Returns404()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");

        var response = await organiser.GetAsync("/api/events/999999/categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
