using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.DTOs;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeachersController : ControllerBase
{
    private readonly TimetableDbContext _context;

    public TeachersController(TimetableDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<TeacherDto>>> GetTeachers()
    {
        var teachers = await _context.Teachers
            .OrderBy(t => t.FullName)
            .Select(t => new TeacherDto
            {
                Id = t.Id,
                FullName = t.FullName
            })
            .ToListAsync();

        return Ok(teachers);
    }

    [HttpPost]
    public async Task<ActionResult<TeacherDto>> AddTeacher([FromBody] TeacherDto input)
    {
        var name = input.FullName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(Invalid("Teacher name is required."));
        if (name.Length > 120)
            return BadRequest(Invalid("Teacher name must be 120 characters or fewer."));
        if (await _context.Teachers.AnyAsync(t => t.FullName.ToLower() == name.ToLower()))
            return Conflict(Invalid("A teacher with that name already exists.", 409));

        var teacher = new Teacher
        {
            FullName = name
        };

        _context.Teachers.Add(teacher);
        await _context.SaveChangesAsync();

        var dto = new TeacherDto
        {
            Id = teacher.Id,
            FullName = teacher.FullName
        };

        return CreatedAtAction(nameof(GetTeachers), dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TeacherDto>> UpdateTeacher(int id, [FromBody] TeacherDto input)
    {
        if (id <= 0) return BadRequest(Invalid("Teacher id must be positive."));
        var name = input.FullName?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(Invalid("Teacher name is required."));
        if (name.Length > 120) return BadRequest(Invalid("Teacher name must be 120 characters or fewer."));
        var teacher = await _context.Teachers.FindAsync(id);
        if (teacher == null) return NotFound();
        if (await _context.Teachers.AnyAsync(t => t.Id != id && t.FullName.ToLower() == name.ToLower()))
            return Conflict(Invalid("A teacher with that name already exists.", 409));
        teacher.FullName = name;
        await _context.SaveChangesAsync();
        return Ok(new TeacherDto { Id = teacher.Id, FullName = teacher.FullName });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTeacher(int id)
    {
        if (id <= 0) return BadRequest(Invalid("Teacher id must be positive."));
        var teacher = await _context.Teachers.FindAsync(id);
        if (teacher == null) return NotFound();
        if (await _context.Lectures.AnyAsync(l => l.TeacherId == id))
            return Conflict(Invalid("This teacher is used by scheduled lectures. Remove those lectures first.", 409));
        _context.Teachers.Remove(teacher);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ProblemDetails Invalid(string detail, int status = 400) =>
        new() { Title = status == 409 ? "Conflict" : "Validation Error", Status = status, Detail = detail };
}
