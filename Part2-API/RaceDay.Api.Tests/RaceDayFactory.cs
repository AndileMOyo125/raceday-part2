using Microsoft.AspNetCore.Mvc.Testing;

namespace RaceDay.Api.Tests;

/// <summary>
/// Starts the real API in memory for the tests. It uses an in-memory database
/// so the tests don't need SQL Server (this also lets them run on GitHub Actions).
/// </summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    static RaceDayFactory()
    {
        // Program.cs reads this setting and switches to the in-memory database
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
    }
}
