using question_answer.Application.DTOs;
using question_answer.Domain.Entities;

namespace question_answer.Application.Services;

public interface IQuestionService
{
    Task<Question> CreateQuestionDraftAsync(ImportedQuestionDto dto, Guid creatorId);
    Task<List<Question>> GetQuestionsForCreatorAsync(Guid creatorId);
    Task<QuestionVersion> EditQuestionVersionAsync(Guid questionId, ImportedQuestionDto dto, Guid creatorId);
    Task<List<QuestionVersion>> GetPendingApprovalsAsync();
    Task<QuestionVersion> ApproveQuestionAsync(Guid questionVersionId, Guid approverId);
    Task<QuestionVersion> RejectQuestionAsync(Guid questionVersionId, Guid approverId, string reason);
}
