using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.DTOs;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeriodsController : ControllerBase
{
    private readonly TimetableDbContext _context;

    public PeriodsController(TimetableDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<PeriodDto>>> GetPeriods()
    {
        var periods = await _context.Periods
            .OrderBy(p => p.StartsAt)
            .Select(p => new PeriodDto
            {
                Id = p.Id,
                Label = p.Label,
                StartsAt = p.StartsAt.ToString("HH:mm"),
                EndsAt = p.EndsAt.ToString("HH:mm")
            })
            .ToListAsync();

        return Ok(periods);
    }

    [HttpPost]
    public async Task<ActionResult<PeriodDto>> AddPeriod([FromBody] PeriodDto input)
    {
        var validation = Validate(input, out var label, out var startsAt, out var endsAt);
        if (validation != null) return BadRequest(validation);
        if (await _context.Periods.AnyAsync(p => p.Label.ToLower() == label.ToLower()))
            return Conflict(Invalid("A period with that label already exists.", 409));
        if (await HasOverlap(startsAt, endsAt, null))
            return Conflict(Invalid("The period overlaps an existing period.", 409));

        var period = new Period
        {
            Label = label,
            StartsAt = startsAt,
            EndsAt = endsAt
        };

        _context.Periods.Add(period);
        await _context.SaveChangesAsync();

        var dto = new PeriodDto
        {
            Id = period.Id,
            Label = period.Label,
            StartsAt = period.StartsAt.ToString("HH:mm"),
            EndsAt = period.EndsAt.ToString("HH:mm")
        };

        return CreatedAtAction(nameof(GetPeriods), dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PeriodDto>> UpdatePeriod(int id, [FromBody] PeriodDto input)
    {
        if (id <= 0) return BadRequest(Invalid("Period id must be positive."));
        var validation = Validate(input, out var label, out var startsAt, out var endsAt);
        if (validation != null) return BadRequest(validation);
        var period = await _context.Periods.FindAsync(id);
        if (period == null) return NotFound();
        if (await _context.Periods.AnyAsync(p => p.Id != id && p.Label.ToLower() == label.ToLower()))
            return Conflict(Invalid("A period with that label already exists.", 409));
        if (await HasOverlap(startsAt, endsAt, id))
            return Conflict(Invalid("The period overlaps an existing period.", 409));
        period.Label = label;
        period.StartsAt = startsAt;
        period.EndsAt = endsAt;
        await _context.SaveChangesAsync();
        return Ok(ToDto(period));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePeriod(int id)
    {
        if (id <= 0) return BadRequest(Invalid("Period id must be positive."));
        var period = await _context.Periods.FindAsync(id);
        if (period == null) return NotFound();
        if (await _context.Lectures.AnyAsync(l => l.PeriodId == id))
            return Conflict(Invalid("This period is used by scheduled lectures. Remove those lectures first.", 409));
        _context.Periods.Remove(period);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> HasOverlap(TimeOnly startsAt, TimeOnly endsAt, int? exceptId) =>
        await _context.Periods.AnyAsync(p =>
            (!exceptId.HasValue || p.Id != exceptId.Value) &&
            startsAt < p.EndsAt && endsAt > p.StartsAt);

    private static ProblemDetails? Validate(PeriodDto input, out string label, out TimeOnly startsAt, out TimeOnly endsAt)
    {
        label = input.Label?.Trim() ?? string.Empty;
        startsAt = default;
        endsAt = default;
        if (string.IsNullOrWhiteSpace(label)) return Invalid("Period label is required.");
        if (label.Length > 30) return Invalid("Period label must be 30 characters or fewer.");
        if (!TimeOnly.TryParse(input.StartsAt, out startsAt)) return Invalid("Invalid start time format. Use HH:mm.");
        if (!TimeOnly.TryParse(input.EndsAt, out endsAt)) return Invalid("Invalid end time format. Use HH:mm.");
        if (endsAt <= startsAt) return Invalid("End time must be after start time.");
        return null;
    }

    private static PeriodDto ToDto(Period period) => new()
    {
        Id = period.Id,
        Label = period.Label,
        StartsAt = period.StartsAt.ToString("HH:mm"),
        EndsAt = period.EndsAt.ToString("HH:mm")
    };

    private static ProblemDetails Invalid(string detail, int status = 400) =>
        new() { Title = status == 409 ? "Conflict" : "Validation Error", Status = status, Detail = detail };
}
