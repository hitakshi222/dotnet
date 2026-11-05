namespace api.DTOs;

public class PeriodDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string StartsAt { get; set; } = string.Empty;
    public string EndsAt { get; set; } = string.Empty;
}
