namespace api.Models;

public class Lecture : IUniversityOwned
{
    public int Id { get; set; }
    public int UniversityId { get; set; }
    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public int DayOfWeek { get; set; }
    public int PeriodId { get; set; }
    public Period Period { get; set; } = null!;
    public int? RoomId { get; set; }
    public Room? Room { get; set; }
    public DateTime CreatedAt { get; set; }
}
