namespace api.DTOs;

public class RoomDto
{
    public int Id { get; set; }
    public int FloorNumber { get; set; }
    public int RoomNumber { get; set; }
    public string Name => $"Floor {FloorNumber}, Room {RoomNumber}";
}
