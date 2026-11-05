namespace api.DTOs;

public class LectureDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public int PeriodId { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }
}
