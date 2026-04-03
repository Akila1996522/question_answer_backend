using question_answer.Application.DTOs;

namespace question_answer.Application.Services;

public interface IAnalyticsService
{
    Task<SuperAdminAnalyticsDto> GetSuperAdminAnalyticsAsync();
    Task<CreatorAnalyticsDto> GetCreatorAnalyticsAsync(Guid creatorId);
}
