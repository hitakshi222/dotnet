using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.DTOs;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly TimetableDbContext _context;

    public SubjectsController(TimetableDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<LookupDto>>> GetSubjects()
    {
        var subjects = await _context.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new LookupDto
            {
                Id = s.Id,
                Name = s.Name
            })
            .ToListAsync();

        return Ok(subjects);
    }

    [HttpPost]
    public async Task<ActionResult<LookupDto>> AddSubject([FromBody] LookupDto input)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(Invalid("Subject name is required."));
        if (name.Length > 100) return BadRequest(Invalid("Subject name must be 100 characters or fewer."));
        if (await _context.Subjects.AnyAsync(s => s.Name.ToLower() == name.ToLower()))
            return Conflict(Invalid("A subject with that name already exists.", 409));

        var subject = new Subject
        {
            Name = name
        };

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        var dto = new LookupDto
        {
            Id = subject.Id,
            Name = subject.Name
        };

        return CreatedAtAction(nameof(GetSubjects), dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LookupDto>> UpdateSubject(int id, [FromBody] LookupDto input)
    {
        if (id <= 0) return BadRequest(Invalid("Subject id must be positive."));
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(Invalid("Subject name is required."));
        if (name.Length > 100) return BadRequest(Invalid("Subject name must be 100 characters or fewer."));
        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null) return NotFound();
        if (await _context.Subjects.AnyAsync(s => s.Id != id && s.Name.ToLower() == name.ToLower()))
            return Conflict(Invalid("A subject with that name already exists.", 409));
        subject.Name = name;
        await _context.SaveChangesAsync();
        return Ok(new LookupDto { Id = subject.Id, Name = subject.Name });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        if (id <= 0) return BadRequest(Invalid("Subject id must be positive."));
        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null) return NotFound();
        if (await _context.Lectures.AnyAsync(l => l.SubjectId == id))
            return Conflict(Invalid("This subject is used by scheduled lectures. Remove those lectures first.", 409));
        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ProblemDetails Invalid(string detail, int status = 400) =>
        new() { Title = status == 409 ? "Conflict" : "Validation Error", Status = status, Detail = detail };
}
