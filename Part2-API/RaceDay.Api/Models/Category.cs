namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    public Event Event { get; set; } = null!;
    public ICollection<EventEnrolment> Enrolments { get; set; } = new List<EventEnrolment>();
}
