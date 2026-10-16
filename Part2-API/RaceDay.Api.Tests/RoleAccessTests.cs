using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

/// <summary>Checks that Organiser only functionality is blocked for Participants and the other way round.</summary>
public class RoleAccessTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public RoleAccessTests(RaceDayFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var (participant, _) = await CreateLoggedInUser("Participant");

        var response = await participant.PostAsJsonAsync("/api/events", ValidEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_Returns403()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, _) = await CreateEventWithCategory(organiser);
        var (participant, _) = await CreateLoggedInUser("Participant");

        var response = await participant.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotAddCategory_Returns403()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, _) = await CreateEventWithCategory(organiser);
        var (participant, _) = await CreateLoggedInUser("Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new { categoryName = "Hacked" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrolInEvent_Returns403()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, category) = await CreateEventWithCategory(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", new { categoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/users/me/enrolments")]
    [InlineData("/api/users/me/results")]
    public async Task Organiser_CannotUseParticipantEndpoints_Returns403(string url)
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");

        var response = await organiser.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_Returns403()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, category) = await CreateEventWithCategory(organiser);
        var (participant, _) = await CreateLoggedInUser("Participant");
        var enrolment = await Enrol(participant, ev.EventId, category.CategoryId);

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results", new
        {
            finishTime = "00:45:00",
            finishPosition = 1
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
