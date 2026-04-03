using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using question_answer.Application.DTOs;
using question_answer.Application.Services;
using System.Security.Claims;

namespace question_answer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "QuestionCreator,SuperAdmin")]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;

    public ExamsController(IExamService examService)
    {
        _examService = examService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> CreateExam([FromBody] ExamCreateDto dto)
    {
        try
        {
            var result = await _examService.CreateExamAsync(dto, GetUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExam(Guid id, [FromBody] ExamCreateDto dto)
    {
        try
        {
            var result = await _examService.UpdateExamAsync(id, dto, GetUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPatch("{id}/publish")]
    public async Task<IActionResult> PublishExam(Guid id)
    {
        try
        {
            await _examService.PublishExamAsync(id, GetUserId());
            return Ok(new { Message = "Exam published successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyExams()
    {
        var result = await _examService.GetExamsForCreatorAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetExamById(Guid id)
    {
        var result = await _examService.GetExamByIdAsync(id, GetUserId());
        if (result == null) return NotFound();
        return Ok(result);
    }
}
