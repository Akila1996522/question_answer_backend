using question_answer.Application.DTOs;
using question_answer.Domain.Entities;
using question_answer.Domain.Enums;

namespace question_answer.Application.Services;

public interface IExamService
{
    Task<Exam> CreateExamAsync(ExamCreateDto dto, Guid creatorId);
    Task<Exam> UpdateExamAsync(Guid examId, ExamCreateDto dto, Guid creatorId);
    Task PublishExamAsync(Guid examId, Guid creatorId);
    Task<List<Exam>> GetExamsForCreatorAsync(Guid creatorId);
    Task<Exam?> GetExamByIdAsync(Guid examId, Guid creatorId);
}
