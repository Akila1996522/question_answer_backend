using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using question_answer.Application.DTOs;
using question_answer.Application.Services;
using System.Security.Claims;

namespace question_answer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly IDocxParserService _docxParser;

    public QuestionsController(IQuestionService questionService, IDocxParserService docxParser)
    {
        _questionService = questionService;
        _docxParser = docxParser;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }

    [HttpPost("preview-import")]
    [Authorize(Roles = "QuestionCreator,SuperAdmin")]
    public IActionResult PreviewImportDocx(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("File is empty");

        try
        {
            using var stream = file.OpenReadStream();
            var result = _docxParser.ParseDocxQuestions(stream);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error parsing document: {ex.Message}");
        }
    }

    [HttpPost("draft")]
    [Authorize(Roles = "QuestionCreator,SuperAdmin")]
    public async Task<IActionResult> CreateDraft([FromBody] ImportedQuestionDto dto)
    {
        var creatorId = GetUserId();
        var result = await _questionService.CreateQuestionDraftAsync(dto, creatorId);
        return Ok(result);
    }

    [HttpGet("my-questions")]
    [Authorize(Roles = "QuestionCreator,SuperAdmin")]
    public async Task<IActionResult> GetMyQuestions()
    {
        var creatorId = GetUserId();
        var result = await _questionService.GetQuestionsForCreatorAsync(creatorId);
        return Ok(result);
    }

    [HttpPut("{id}/version")]
    [Authorize(Roles = "QuestionCreator,SuperAdmin")]
    public async Task<IActionResult> EditQuestionVersion(Guid id, [FromBody] ImportedQuestionDto dto)
    {
        var creatorId = GetUserId();
        var result = await _questionService.EditQuestionVersionAsync(id, dto, creatorId);
        return Ok(result);
    }

    [HttpGet("pending-approvals")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetPendingApprovals()
    {
        var results = await _questionService.GetPendingApprovalsAsync();
        return Ok(results);
    }

    [HttpPatch("approve/{versionId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ApproveQuestion(Guid versionId)
    {
        var approverId = GetUserId();
        var result = await _questionService.ApproveQuestionAsync(versionId, approverId);
        return Ok(result);
    }

    [HttpPatch("reject/{versionId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RejectQuestion(Guid versionId, [FromBody] string reason)
    {
        var approverId = GetUserId();
        var result = await _questionService.RejectQuestionAsync(versionId, approverId, reason);
        return Ok(result);
    }
}
