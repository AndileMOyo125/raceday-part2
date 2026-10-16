using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// The unit tests switch this on so they run against an in-memory database instead of SQL Server
var useInMemoryDatabase = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var inMemoryDatabaseName = Guid.NewGuid().ToString();

builder.Services.AddDbContext<RaceDayContext>(options =>
{
    if (useInMemoryDatabase)
    {
        options.UseInMemoryDatabase(inMemoryDatabaseName);
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb"));
    }
});

builder.Services.AddControllers();

// Session management keeps the user's logged in state and role between requests
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(60);
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "REST API for the RaceDay event management system. " +
                      "Register and log in first (POST /api/auth/register then POST /api/auth/login). " +
                      "The session cookie is then sent automatically, so you can try the other endpoints from this page."
    });

    // Pulls the /// comments from the controllers into Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
    if (useInMemoryDatabase)
    {
        db.Database.EnsureCreated();
    }
    else
    {
        // applies the EF Core migrations, creating the database if it doesn't exist yet
        db.Database.Migrate();
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseSession();
app.MapControllers();

app.Run();

// needed so the test project can start the API
public partial class Program { }
