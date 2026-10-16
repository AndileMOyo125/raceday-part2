using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Tests;

/// <summary>Participants enrolling in events and Organisers capturing results.</summary>
public class EnrolmentAndResultTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public EnrolmentAndResultTests(RaceDayFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Participant_CanEnrol_AndTheEnrolmentIsRecorded()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, category) = await CreateEventWithCategory(organiser);
        var (participant, participantUser) = await CreateLoggedInUser("Participant");

        var enrolment = await Enrol(participant, ev.EventId, category.CategoryId);

        // the link between participant, event and category is recorded
        Assert.Equal(participantUser.UserId, enrolment.ParticipantId);
        Assert.Equal(ev.EventId, enrolment.EventId);
        Assert.Equal(category.CategoryId, enrolment.CategoryId);

        // the participant sees it in their own history
        var mine = await participant.GetFromJsonAsync<List<EnrolmentResponse>>("/api/users/me/enrolments");
        Assert.Contains(mine!, x => x.EnrolmentId == enrolment.EnrolmentId);

        // the organiser sees it in the event's enrolments
        var forEvent = await organiser.GetFromJsonAsync<List<EnrolmentResponse>>($"/api/events/{ev.EventId}/enrolments");
        Assert.Contains(forEvent!, x => x.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInTheSameEvent_Returns409()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, category) = await CreateEventWithCategory(organiser);
        var (participant, _) = await CreateLoggedInUser("Participant");
        await Enrol(participant, ev.EventId, category.CategoryId);

        var second = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", new { categoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromAnotherEvent_Returns400()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (eventA, _) = await CreateEventWithCategory(organiser);
        var (_, categoryB) = await CreateEventWithCategory(organiser);
        var (participant, _) = await CreateLoggedInUser("Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{eventA.EventId}/enrolments", new { categoryId = categoryB.CategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InEventThatDoesNotExist_Returns404()
    {
        var (participant, _) = await CreateLoggedInUser("Participant");

        var response = await participant.PostAsJsonAsync("/api/events/999999/enrolments", new { categoryId = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryThatDoesNotExist_Returns404()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, _) = await CreateEventWithCategory(organiser);
        var (participant, _) = await CreateLoggedInUser("Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", new { categoryId = 999999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsOfAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await CreateLoggedInUser("Organiser");
        var (ev, _) = await CreateEventWithCategory(owner);
        var (otherOrganiser, _) = await CreateLoggedInUser("Organiser");

        var response = await otherOrganiser.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Category_WithEnrolments_CannotBeDeleted_Returns409()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, category) = await CreateEventWithCategory(organiser);
        var (participant, _) = await CreateLoggedInUser("Participant");
        await Enrol(participant, ev.EventId, category.CategoryId);

        var response = await organiser.DeleteAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewIt()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, category) = await CreateEventWithCategory(organiser);
        var (participant, participantUser) = await CreateLoggedInUser("Participant");
        var enrolment = await Enrol(participant, ev.EventId, category.CategoryId);

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results", new
        {
            finishTime = "00:48:32",
            finishPosition = 1
        });

        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);
        var result = (await capture.Content.ReadFromJsonAsync<ResultResponse>())!;
        Assert.Equal(1, result.FinishPosition);
        Assert.Equal(new TimeSpan(0, 48, 32), result.FinishTime);
        Assert.Equal(participantUser.UserId, result.ParticipantId);

        // participant sees their own result
        var mine = await participant.GetFromJsonAsync<List<ResultResponse>>("/api/users/me/results");
        Assert.Contains(mine!, r => r.ResultId == result.ResultId);

        // results for the event
        var forEvent = await participant.GetFromJsonAsync<List<ResultResponse>>($"/api/events/{ev.EventId}/results");
        Assert.Contains(forEvent!, r => r.ResultId == result.ResultId);
    }

    [Fact]
    public async Task Participant_OnlySeesTheirOwnResults()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, category) = await CreateEventWithCategory(organiser);
        var (participantA, _) = await CreateLoggedInUser("Participant");
        var (participantB, _) = await CreateLoggedInUser("Participant");
        var enrolmentA = await Enrol(participantA, ev.EventId, category.CategoryId);
        await Enrol(participantB, ev.EventId, category.CategoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentA.EnrolmentId}/results", new { finishTime = "00:50:00", finishPosition = 1 });

        var resultsForB = await participantB.GetFromJsonAsync<List<ResultResponse>>("/api/users/me/results");

        Assert.Empty(resultsForB!);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, category) = await CreateEventWithCategory(organiser);
        var (participant, _) = await CreateLoggedInUser("Participant");
        var enrolment = await Enrol(participant, ev.EventId, category.CategoryId);
        var body = new { finishTime = "00:50:00", finishPosition = 1 };

        var first = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results", body);
        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForEnrolmentThatDoesNotExist_Returns404()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/enrolments/999999/results", new { finishTime = "00:50:00", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithInvalidPosition_Returns400()
    {
        var (organiser, _) = await CreateLoggedInUser("Organiser");
        var (ev, category) = await CreateEventWithCategory(organiser);
        var (participant, _) = await CreateLoggedInUser("Participant");
        var enrolment = await Enrol(participant, ev.EventId, category.CategoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results", new { finishTime = "00:50:00", finishPosition = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResultForAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await CreateLoggedInUser("Organiser");
        var (ev, category) = await CreateEventWithCategory(owner);
        var (participant, _) = await CreateLoggedInUser("Participant");
        var enrolment = await Enrol(participant, ev.EventId, category.CategoryId);
        var (otherOrganiser, _) = await CreateLoggedInUser("Organiser");

        var response = await otherOrganiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results", new { finishTime = "00:50:00", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
