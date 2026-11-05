namespace api.DTOs;

public class ClashDto
{
    public int DayOfWeek { get; set; }
    public string Rule { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int ConflictingLectureId { get; set; }
}
