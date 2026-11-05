namespace api.Models;

public class University
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
}

public interface IUniversityOwned
{
    int UniversityId { get; set; }
}
