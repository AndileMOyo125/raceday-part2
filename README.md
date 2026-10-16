# RaceDay

RaceDay is a full-stack, web-based event management system built for the South African road running, walking and cycling community. A lot of local events (park runs, charity walks, club races) are still run off paper registers, spreadsheets and WhatsApp groups, so this project digitises that process. Organisers can create and manage events, set up age/distance categories, and capture results. Participants can browse upcoming events, enter them, track their own results history, and (in a later part of this project) check the weather and route for race day.

The project is built in three parts:

| Part | What it covers | Where it lives |
|------|----------------|----------------|
| Part 1 | Planning: ERD, API endpoint plan, SQL script | `/docs` |
| Part 2 | RESTful API (ASP.NET Core Web API, EF Core, session auth, Swagger, unit tests, CI/CD) | `/Part2-API` |
| Part 3 | MVC web application, Azure Blob Storage, Docker | not started yet |

## Roles

The system has two roles, chosen when a user registers:

- **Organiser** – creates, updates and deletes their own events, defines the age/distance categories for each event, views who has enrolled, and captures finish times and positions after the event.
- **Participant** – browses events, enrols in an event under a chosen category, and views their own enrolment and results history. Participants can't manage events or capture results.

Both roles can view events and categories, and view and update their own profile.

---

# Part 1 – System Planning and Database

## Part 1 contents (`/docs`)

- `ERD_RaceDay.png` – entity relationship diagram (6 entities: Roles, Users, Events, Categories, EventEnrolments, Results).
- `API_Endpoint_Plan.pdf` – full list of planned API endpoints.
- `RaceDay_Database_Script.sql` – SQL Server script that creates the schema and seeds sample data.

## Part 1 CI/CD

A GitHub Actions workflow (`validate-repo.yml`) runs on push and checks that the `/docs` folder exists and contains the required planning files.

Screenshot of a successful build:

<img width="1363" height="717" alt="image" src="https://github.com/user-attachments/assets/6c6148f5-25b9-4713-9d9b-143ace781f9c" />

## Part 1 video walkthrough

https://youtu.be/2loYNUHwaPg?si=6ZT5BwveyANaIagE

---

# Part 2 – RESTful API Development

The API lives in `/Part2-API`:

```
Part2-API/
├── RaceDay.sln
├── RaceDay.Api/            the Web API project
│   ├── Controllers/        Auth, Users, Events, Categories, Enrolments, Results
│   ├── Models/             EF Core entity classes (match the Part 1 ERD)
│   ├── Data/               RaceDayContext (EF Core DbContext + Fluent API config)
│   ├── Dtos/               request and response objects + query projections
│   ├── Helpers/            password hashing, session helpers, role authorisation attribute
│   ├── Migrations/         EF Core code-first migrations
│   └── Program.cs          services, session, Swagger and pipeline setup
└── RaceDay.Api.Tests/      xUnit tests
```

## Tech used

- ASP.NET Core Web API (C#, .NET 8)
- Entity Framework Core 8 (Code-First, SQL Server)
- Session based authentication (cookie + server side session)
- Swagger / Swashbuckle
- xUnit with `WebApplicationFactory` for the tests
- GitHub Actions for CI

## Setup and run instructions

**You need:** .NET 8 SDK, SQL Server (Express or LocalDB), and the EF Core 8 tool (`dotnet tool install --global dotnet-ef --version 8.0.11`).

1. Clone the repository and open the `Part2-API` folder:
   ```bash
   git clone <your repository link>
   cd RaceDay-POE/Part2-API
   ```
2. Open `RaceDay.Api/appsettings.json` and check the connection string. Change the `Server=` part to match your own SQL Server instance (for example `localhost\SQLEXPRESS` or `(localdb)\MSSQLLocalDB`). The API uses its own database called `RaceDayApiDB`, so it doesn't clash with the `RaceDayDB` created by the Part 1 script.
3. Restore and build:
   ```bash
   dotnet restore
   dotnet build
   ```
4. Run the API. The database and tables are created automatically on startup by the EF Core migrations:
   ```bash
   dotnet run --project RaceDay.Api
   ```
5. Open Swagger in your browser: **http://localhost:5080/swagger**

### Trying it out in Swagger

1. Call `POST /api/auth/register` to create an Organiser (`"role": "Organiser"`) and another account as a Participant.
2. Call `POST /api/auth/login`. The session cookie is stored by the browser, so every request from Swagger after this is authenticated.
3. Try the other endpoints. To switch role, call `POST /api/auth/logout` and log in as the other user.

### Run the unit tests

```bash
dotnet test
```

The tests start the real API in memory with an in-memory database, so SQL Server isn't needed to run them (this is also what lets them run on GitHub Actions).

## How authentication and roles work

- A user registers with their details and picks a role (`Organiser` or `Participant`). The role must be one of those two, otherwise the API returns 400.
- Passwords are hashed with PBKDF2 and a random salt before saving. The original password is never stored.
- On login, the API starts a session and stores the user's id and role in it. The browser holds a session cookie, and the API reads the user and role from the session on every request.
- A `[SessionAuthorize]` attribute protects the endpoints. No session gives **401 Unauthorized**; a logged in user with the wrong role gives **403 Forbidden**.
- Organisers can only change or view data for events they created (checked using the event's `OrganiserID`).

## Endpoints

| Area | Method and route | Role |
|------|------------------|------|
| Auth | `POST /api/auth/register` | public |
| Auth | `POST /api/auth/login` | public |
| Auth | `POST /api/auth/logout` | public |
| Profile | `GET /api/users/me` | any logged in |
| Profile | `PUT /api/users/me` | any logged in |
| Events | `GET /api/events`, `GET /api/events/{id}` | any logged in |
| Events | `POST /api/events`, `PUT /api/events/{id}`, `DELETE /api/events/{id}` | Organiser |
| Categories | `GET /api/events/{eventId}/categories` | any logged in |
| Categories | `POST /api/events/{eventId}/categories`, `PUT /api/categories/{id}`, `DELETE /api/categories/{id}` | Organiser |
| Enrolments | `POST /api/events/{eventId}/enrolments` | Participant |
| Enrolments | `GET /api/events/{eventId}/enrolments` | Organiser |
| Enrolments | `GET /api/users/me/enrolments` | Participant |
| Results | `POST /api/enrolments/{enrolmentId}/results` | Organiser |
| Results | `GET /api/events/{eventId}/results` | any logged in |
| Results | `GET /api/users/me/results` | Participant |

## API plan and ERD

The implemented API matches the endpoint plan in `/docs/API_Endpoint_Plan.pdf` (every route, method, role and response code in the plan is implemented, and the other way round). The EF Core migration matches the Part 1 ERD and SQL script's six tables, columns, SQL Server data types, keys, relationships, defaults and check constraint. SQL uniqueness rules are represented as unique indexes in EF Core; these enforce the same uniqueness requirements. The migration seeds the two required roles, while the Part 1 script's sample users and events are not copied into the API database.

At the start of Part 2 the plan was revised in its own commit to describe session based authentication (the Part 2 brief requires session management, and the first draft described a token), and to add the logout endpoint and the extra error responses.

## Part 2 CI/CD

A GitHub Actions workflow (`.github/workflows/dotnet-ci.yml`) restores, builds the project and runs all unit tests on every push.

Screenshot of a successful build:

![green build
](image.png)

## Part 2 video walkthrough

Unlisted YouTube video demonstrating the running API, the code structure, endpoints, authentication and unit tests:

`[INSERT PART 2 YOUTUBE LINK HERE]`

## License

This project is licensed under the [MIT License](LICENSE).
