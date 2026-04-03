using question_answer.Application.DTOs;
using question_answer.Domain.Entities;

namespace question_answer.Application.Services;

public interface IExamTakingService
{
    Task<ExamAttempt> StartAttemptAsync(StartAttemptDto dto, Guid userId);
    Task<AttemptQuestionDto> SubmitAnswerAsync(Guid attemptId, SubmitAnswerDto dto, Guid userId);
    Task<FinishResultDto> FinishAttemptAsync(Guid attemptId, Guid userId);
    Task<ExamStatusDto> GetAttemptStatusAsync(Guid attemptId, Guid userId);
}
