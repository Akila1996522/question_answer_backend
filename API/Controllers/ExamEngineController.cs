using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using question_answer.Application.DTOs;
using question_answer.Application.Services;
using System.Security.Claims;

namespace question_answer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ExamFacer,SuperAdmin")]
public class ExamEngineController : ControllerBase
{
    private readonly IExamTakingService _examTakingService;

    public ExamEngineController(IExamTakingService examTakingService)
    {
        _examTakingService = examTakingService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartAttempt([FromBody] StartAttemptDto dto)
    {
        try
        {
            var attempt = await _examTakingService.StartAttemptAsync(dto, GetUserId());
            return Ok(new { AttemptId = attempt.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("{attemptId}/status")]
    public async Task<IActionResult> GetStatus(Guid attemptId)
    {
        try
        {
            var status = await _examTakingService.GetAttemptStatusAsync(attemptId, GetUserId());
            return Ok(status);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("{attemptId}/submit-answer")]
    public async Task<IActionResult> SubmitAnswer(Guid attemptId, [FromBody] SubmitAnswerDto dto)
    {
        try
        {
            var result = await _examTakingService.SubmitAnswerAsync(attemptId, dto, GetUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("{attemptId}/finish")]
    public async Task<IActionResult> FinishAttempt(Guid attemptId)
    {
        try
        {
            var result = await _examTakingService.FinishAttemptAsync(attemptId, GetUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
