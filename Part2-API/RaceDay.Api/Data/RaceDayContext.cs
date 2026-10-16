using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// EF Core context. The table names, column names and constraints below are set up
/// to match the Part 1 ERD and SQL script.
/// </summary>
public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventEnrolment> EventEnrolments => Set<EventEnrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Roles ----
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(r => r.RoleId);
            e.Property(r => r.RoleId).HasColumnName("RoleID");
            e.Property(r => r.RoleName).HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
            e.HasIndex(r => r.RoleName).IsUnique();

            // The two roles always exist, so they are seeded here
            e.HasData(
                new Role { RoleId = 1, RoleName = "Organiser" },
                new Role { RoleId = 2, RoleName = "Participant" });
        });

        // ---- Users ----
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.UserId);
            e.Property(u => u.UserId).HasColumnName("UserID");
            e.Property(u => u.RoleId).HasColumnName("RoleID");
            e.Property(u => u.FullName).HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
            e.Property(u => u.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasIndex(u => u.Email).IsUnique();

            e.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Users_Roles");
        });

        // ---- Events ----
        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events", t => t.HasCheckConstraint("CK_Events_EventType", "EventType IN ('Run','Walk','Cycle')"));
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventId).HasColumnName("EventID");
            e.Property(x => x.OrganiserId).HasColumnName("OrganiserID");
            e.Property(x => x.EventName).HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasColumnType("varchar(500)").HasMaxLength(500);
            e.Property(x => x.EventDate).HasColumnType("datetime");
            e.Property(x => x.Location).HasColumnType("varchar(150)").HasMaxLength(150).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(5, 2);
            e.Property(x => x.EventType).HasColumnType("varchar(20)").HasMaxLength(20).IsRequired().HasDefaultValue("Run");

            e.HasOne(x => x.Organiser)
                .WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Events_Users");
        });

        // ---- Categories ----
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.CategoryId).HasColumnName("CategoryID");
            e.Property(c => c.EventId).HasColumnName("EventID");
            e.Property(c => c.CategoryName).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();

            e.HasOne(c => c.Event)
                .WithMany(ev => ev.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Categories_Events");
        });

        // ---- EventEnrolments ----
        modelBuilder.Entity<EventEnrolment>(e =>
        {
            e.ToTable("EventEnrolments");
            e.HasKey(x => x.EnrolmentId);
            e.Property(x => x.EnrolmentId).HasColumnName("EnrolmentID");
            e.Property(x => x.ParticipantId).HasColumnName("ParticipantID");
            e.Property(x => x.EventId).HasColumnName("EventID");
            e.Property(x => x.CategoryId).HasColumnName("CategoryID");
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");

            // a participant can only enrol once per event
            e.HasIndex(x => new { x.ParticipantId, x.EventId }).IsUnique().HasDatabaseName("UQ_Enrolment");

            e.HasOne(x => x.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Enrolments_Users");

            e.HasOne(x => x.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Enrolments_Events");

            e.HasOne(x => x.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Enrolments_Categories");
        });

        // ---- Results ----
        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(r => r.ResultId);
            e.Property(r => r.ResultId).HasColumnName("ResultID");
            e.Property(r => r.EnrolmentId).HasColumnName("EnrolmentID");

            // one result per enrolment (this also creates the unique index)
            e.HasOne(r => r.Enrolment)
                .WithOne(en => en.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Results_Enrolments");
        });
    }
}
