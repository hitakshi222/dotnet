namespace api.Models;

public class Subject : IUniversityOwned
{
    public int Id { get; set; }
    public int UniversityId { get; set; }
    public string Name { get; set; } = string.Empty;
}
