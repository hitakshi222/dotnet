namespace api.Models;

public class Period : IUniversityOwned
{
    public int Id { get; set; }
    public int UniversityId { get; set; }
    public string Label { get; set; } = string.Empty;
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
}
