using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace question_answer.Application.Services;

public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLogMiddleware> _logger;

    public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, question_answer.Infrastructure.Data.AppDbContext dbContext)
    {
        var originalBodyStream = context.Response.Body;

        try
        {
            await _next(context);

            if (context.User.Identity?.IsAuthenticated == true && (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "PATCH" || context.Request.Method == "DELETE"))
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    var auditLog = new question_answer.Domain.Entities.AuditLog
                    {
                        UserId = userId,
                        Action = $"{context.Request.Method} {context.Request.Path}",
                        EntityName = "API Route",
                        Details = $"Status Code: {context.Response.StatusCode}"
                    };

                    dbContext.AuditLogs.Add(auditLog);
                    await dbContext.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred handling the request.");
            throw;
        }
    }
}
