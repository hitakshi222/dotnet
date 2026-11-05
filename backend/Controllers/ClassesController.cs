using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.DTOs;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassesController : ControllerBase
{
    private readonly TimetableDbContext _context;

    public ClassesController(TimetableDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<LookupDto>>> GetClasses()
    {
        var classes = await _context.Classes
            .OrderBy(c => c.Name)
            .Select(c => new LookupDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();

        return Ok(classes);
    }

    [HttpPost]
    public async Task<ActionResult<LookupDto>> AddClass([FromBody] LookupDto input)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(Invalid("Class name is required."));
        if (name.Length > 80) return BadRequest(Invalid("Class name must be 80 characters or fewer."));
        if (await _context.Classes.AnyAsync(c => c.Name.ToLower() == name.ToLower()))
            return Conflict(Invalid("A class with that name already exists.", 409));

        var @class = new Class
        {
            Name = name
        };

        _context.Classes.Add(@class);
        await _context.SaveChangesAsync();

        var dto = new LookupDto
        {
            Id = @class.Id,
            Name = @class.Name
        };

        return CreatedAtAction(nameof(GetClasses), dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LookupDto>> UpdateClass(int id, [FromBody] LookupDto input)
    {
        if (id <= 0) return BadRequest(Invalid("Class id must be positive."));
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(Invalid("Class name is required."));
        if (name.Length > 80) return BadRequest(Invalid("Class name must be 80 characters or fewer."));
        var @class = await _context.Classes.FindAsync(id);
        if (@class == null) return NotFound();
        if (await _context.Classes.AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower()))
            return Conflict(Invalid("A class with that name already exists.", 409));
        @class.Name = name;
        await _context.SaveChangesAsync();
        return Ok(new LookupDto { Id = @class.Id, Name = @class.Name });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteClass(int id)
    {
        if (id <= 0) return BadRequest(Invalid("Class id must be positive."));
        var @class = await _context.Classes.FindAsync(id);
        if (@class == null) return NotFound();
        if (await _context.Lectures.AnyAsync(l => l.ClassId == id))
            return Conflict(Invalid("This class is used by scheduled lectures. Remove those lectures first.", 409));
        _context.Classes.Remove(@class);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ProblemDetails Invalid(string detail, int status = 400) =>
        new() { Title = status == 409 ? "Conflict" : "Validation Error", Status = status, Detail = detail };
}
