namespace api.Models;

public class Teacher : IUniversityOwned
{
    public int Id { get; set; }
    public int UniversityId { get; set; }
    public string FullName { get; set; } = string.Empty;
}
