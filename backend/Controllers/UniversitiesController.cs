using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("api/universities")]
public class UniversitiesController : ControllerBase
{
    private readonly TimetableDbContext _context;

    public UniversitiesController(TimetableDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UniversityDto>>> GetUniversities()
    {
        var universities = await _context.Universities.AsNoTracking()
            .OrderBy(university => university.Name)
            .ToListAsync();
        return Ok(universities.Select(ToDto));
    }

    [HttpPost]
    public async Task<ActionResult<UniversityDto>> CreateUniversity([FromBody] UniversityInput input)
    {
        var name = input.Name.Trim();
        if (await _context.Universities.AnyAsync(university => university.Name.ToLower() == name.ToLower()))
            return Conflict(new ProblemDetails { Title = "University already exists", Status = 409, Detail = "A university with this name is already registered." });

        var university = new University
        {
            Name = name,
            Country = input.Country.Trim(),
            State = input.State.Trim(),
            District = input.District.Trim(),
            City = input.City.Trim(),
            ContactEmail = input.ContactEmail.Trim()
        };

        _context.Universities.Add(university);
        await _context.SaveChangesAsync();
        var dto = ToDto(university);
        return CreatedAtAction(nameof(GetUniversities), null, dto);
    }

    private static UniversityDto ToDto(University university) => new(
        university.Id, university.Name, university.Country, university.State,
        university.District, university.City, university.ContactEmail);
}

public sealed record UniversityDto(int Id, string Name, string Country, string State, string District, string City, string ContactEmail);

public sealed class UniversityInput
{
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(100)] public string Country { get; init; } = string.Empty;
    [Required, StringLength(100)] public string State { get; init; } = string.Empty;
    [Required, StringLength(100)] public string District { get; init; } = string.Empty;
    [Required, StringLength(100)] public string City { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string ContactEmail { get; init; } = string.Empty;
}
