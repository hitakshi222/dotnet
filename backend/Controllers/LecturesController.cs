using Microsoft.AspNetCore.Mvc;
using api.DTOs;
using api.Services;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LecturesController : ControllerBase
{
    private readonly LectureService _lectureService;

    public LecturesController(LectureService lectureService)
    {
        _lectureService = lectureService;
    }

    [HttpPost]
    public async Task<ActionResult<List<LectureDto>>> CreateLectures([FromBody] LectureInputDto input)
    {
        var (savedLectures, errors, clashes) = await _lectureService.SaveLecturesAsync(input);

        if (errors != null && errors.Any())
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Status = 400,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Detail = "One or more validation errors occurred.",
                Extensions = new Dictionary<string, object?>
                {
                    ["errors"] = new Dictionary<string, string[]>
                    {
                        ["days"] = errors.ToArray()
                    }
                }
            });
        }

        if (clashes != null && clashes.Any())
        {
            return Conflict(new ProblemDetails
            {
                Title = "Clash",
                Status = 409,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Detail = "Nothing was saved.",
                Extensions = new Dictionary<string, object?>
                {
                    ["clashes"] = clashes
                }
            });
        }

        return CreatedAtAction(nameof(CreateLectures), savedLectures);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LectureDto>> UpdateLecture(int id, [FromBody] LectureInputDto input)
    {
        if (id <= 0)
            return BadRequest(ValidationProblem("Lecture id must be a positive number.", "id"));

        var (lecture, errors, clashes) = await _lectureService.UpdateLectureAsync(id, input);
        if (errors?.Contains("Lecture not found.") == true)
            return NotFound(new ProblemDetails { Title = "Not Found", Status = 404, Detail = "Lecture not found." });
        if (errors is { Count: > 0 })
            return BadRequest(ValidationProblem(errors, "lecture"));
        if (clashes is { Count: > 0 })
            return Conflict(new ProblemDetails
            {
                Title = "Clash",
                Status = 409,
                Detail = "The lecture was not updated.",
                Extensions = new Dictionary<string, object?> { ["clashes"] = clashes }
            });
        return Ok(lecture);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteLecture(int id)
    {
        if (id <= 0)
            return BadRequest(ValidationProblem("Lecture id must be a positive number.", "id"));
        return await _lectureService.DeleteLectureAsync(id) ? NoContent() : NotFound();
    }

    [HttpGet]
    public async Task<ActionResult<List<LectureDto>>> GetTimetable([FromQuery] int? classId, [FromQuery] int? teacherId)
    {
        if (classId <= 0 || teacherId <= 0)
        {
            return BadRequest(ValidationProblem("Filter ids must be positive numbers.", "query"));
        }

        if (classId.HasValue && teacherId.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Status = 400,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Detail = "Cannot specify both classId and teacherId.",
                Extensions = new Dictionary<string, object?>
                {
                    ["errors"] = new Dictionary<string, string[]>
                    {
                        ["query"] = new[] { "Specify either classId or teacherId, not both." }
                    }
                }
            });
        }

        var lectures = await _lectureService.GetTimetableAsync(classId, teacherId);
        return Ok(lectures);
    }

    private static ProblemDetails ValidationProblem(string error, string key) => ValidationProblem(new[] { error }, key);

    private static ProblemDetails ValidationProblem(IEnumerable<string> errors, string key)
    {
        var problem = new ProblemDetails { Title = "Validation Error", Status = 400, Detail = "The request is invalid." };
        problem.Extensions["errors"] = new Dictionary<string, string[]> { [key] = errors.ToArray() };
        return problem;
    }
}
