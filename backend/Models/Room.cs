namespace api.Models;

public class Room : IUniversityOwned
{
    public int Id { get; set; }
    public int UniversityId { get; set; }
    public int FloorNumber { get; set; }
    public int RoomNumber { get; set; }
    public string Name => $"Floor {FloorNumber}, Room {RoomNumber}";
}
