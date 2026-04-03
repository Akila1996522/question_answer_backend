using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using question_answer.Application.Services;
using System.Security.Claims;

namespace question_answer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }

    [HttpGet("superadmin")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetSuperAdminStats()
    {
        var stats = await _analyticsService.GetSuperAdminAnalyticsAsync();
        return Ok(stats);
    }

    [HttpGet("creator")]
    [Authorize(Roles = "QuestionCreator,SuperAdmin")]
    public async Task<IActionResult> GetCreatorStats()
    {
        var stats = await _analyticsService.GetCreatorAnalyticsAsync(GetUserId());
        return Ok(stats);
    }
}
