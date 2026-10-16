using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Tests;

/// <summary>User registration, login, logout and the session.</summary>
public class AuthTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public AuthTests(RaceDayFactory factory) : base(factory)
    {
    }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_WithValidDetails_Returns201(string role)
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "New User",
            email = $"{Guid.NewGuid():N}@raceday.test",
            password = "Password1!",
            role
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(role, user!.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        var client = Factory.CreateClient();
        var body = new
        {
            fullName = "Duplicate User",
            email = $"{Guid.NewGuid():N}@raceday.test",
            password = "Password1!",
            role = "Participant"
        };

        var first = await client.PostAsJsonAsync("/api/auth/register", body);
        var second = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithUnknownRole_Returns400()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Bad Role",
            email = $"{Guid.NewGuid():N}@raceday.test",
            password = "Password1!",
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Short Password",
            email = $"{Guid.NewGuid():N}@raceday.test",
            password = "123",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200AndStartsSession()
    {
        var (client, user) = await CreateLoggedInUser("Participant");

        // the session cookie should now let the client call a protected endpoint
        var profile = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var body = await profile.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(user.UserId, body!.UserId);
        Assert.Equal("Participant", body.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = Factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@raceday.test";
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Wrong Password",
            email,
            password = "Password1!",
            role = "Participant"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "NotMyPassword" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@raceday.test",
            password = "Password1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/users/me")]
    [InlineData("/api/users/me/enrolments")]
    [InlineData("/api/users/me/results")]
    public async Task ProtectedEndpoints_WithoutLogin_Return401(string url)
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var (client, _) = await CreateLoggedInUser("Organiser");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        var afterLogout = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ChangesNameAndEmail()
    {
        var (client, _) = await CreateLoggedInUser("Participant");
        var newEmail = $"{Guid.NewGuid():N}@raceday.test";

        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "Updated Name", email = newEmail });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("Updated Name", user!.FullName);
        Assert.Equal(newEmail, user.Email);
    }

    [Fact]
    public async Task UpdateProfile_WithEmailOfAnotherUser_Returns400()
    {
        var (_, otherUser) = await CreateLoggedInUser("Participant");
        var (client, _) = await CreateLoggedInUser("Participant");

        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "Someone", email = otherUser.Email });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_JsonIsAvailable()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
