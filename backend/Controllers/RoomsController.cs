using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.DTOs;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly TimetableDbContext _context;

    public RoomsController(TimetableDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetRooms()
    {
        var rooms = await _context.Rooms
            .OrderBy(room => room.FloorNumber)
            .ThenBy(room => room.RoomNumber)
            .Select(room => new RoomDto
            {
                Id = room.Id,
                FloorNumber = room.FloorNumber,
                RoomNumber = room.RoomNumber
            })
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpPost]
    public async Task<ActionResult<RoomDto>> AddRoom([FromBody] RoomInputDto input)
    {
        var validation = Validate(input, out var floorNumber, out var roomNumber);
        if (validation != null) return BadRequest(validation);
        if (await _context.Rooms.AnyAsync(room =>
                room.FloorNumber == floorNumber && room.RoomNumber == roomNumber))
            return Conflict(Invalid("That infrastructure location already exists.", 409));

        var room = new Room { FloorNumber = floorNumber, RoomNumber = roomNumber };
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRooms), ToDto(room));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomDto>> UpdateRoom(int id, [FromBody] RoomInputDto input)
    {
        if (id <= 0) return BadRequest(Invalid("Infrastructure id must be positive."));
        var validation = Validate(input, out var floorNumber, out var roomNumber);
        if (validation != null) return BadRequest(validation);
        var room = await _context.Rooms.FindAsync(id);
        if (room == null) return NotFound();
        if (await _context.Rooms.AnyAsync(existing =>
                existing.Id != id &&
                existing.FloorNumber == floorNumber &&
                existing.RoomNumber == roomNumber))
            return Conflict(Invalid("That infrastructure location already exists.", 409));

        room.FloorNumber = floorNumber;
        room.RoomNumber = roomNumber;
        await _context.SaveChangesAsync();
        return Ok(ToDto(room));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        if (id <= 0) return BadRequest(Invalid("Infrastructure id must be positive."));
        var room = await _context.Rooms.FindAsync(id);
        if (room == null) return NotFound();
        if (await _context.Lectures.AnyAsync(lecture => lecture.RoomId == id))
            return Conflict(Invalid("This infrastructure location is used by scheduled lectures. Remove or reassign those lectures first.", 409));
        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ProblemDetails? Validate(RoomInputDto input, out int floorNumber, out int roomNumber)
    {
        floorNumber = input.FloorNumber ?? 0;
        roomNumber = input.RoomNumber ?? 0;
        if (!input.FloorNumber.HasValue) return Invalid("Floor number is required.");
        if (floorNumber < 0) return Invalid("Floor number must be zero or greater.");
        if (!input.RoomNumber.HasValue) return Invalid("Room number is required.");
        if (roomNumber <= 0) return Invalid("Room number must be greater than zero.");
        return null;
    }

    private static RoomDto ToDto(Room room) => new()
    {
        Id = room.Id,
        FloorNumber = room.FloorNumber,
        RoomNumber = room.RoomNumber
    };

    private static ProblemDetails Invalid(string detail, int status = 400) =>
        new() { Title = status == 409 ? "Conflict" : "Validation Error", Status = status, Detail = detail };
}
