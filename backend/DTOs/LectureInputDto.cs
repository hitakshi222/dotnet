namespace api.DTOs;

public class LectureInputDto
{
    public int TeacherId { get; set; }
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public int PeriodId { get; set; }
    public int RoomId { get; set; }
    public List<int> Days { get; set; } = new();
}
