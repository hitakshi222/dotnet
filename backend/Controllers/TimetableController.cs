using Microsoft.AspNetCore.Mvc;
using api.DTOs;
using api.Services;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimetableController : ControllerBase
{
    private readonly LectureService _lectureService;

    public TimetableController(LectureService lectureService)
    {
        _lectureService = lectureService;
    }

    [HttpGet]
    public async Task<ActionResult<List<LectureDto>>> GetTimetable([FromQuery] int? classId, [FromQuery] int? teacherId)
    {
        if (classId <= 0 || teacherId <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Status = 400,
                Detail = "Filter ids must be positive numbers."
            });
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
}
