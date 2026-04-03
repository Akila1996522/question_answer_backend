using Microsoft.EntityFrameworkCore;
using question_answer.Application.DTOs;
using question_answer.Domain.Entities;
using question_answer.Domain.Enums;
using question_answer.Infrastructure.Data;

namespace question_answer.Application.Services;

public class ExamService : IExamService
{
    private readonly AppDbContext _context;

    public ExamService(AppDbContext context)
    {
        _context = context;
    }

    private async Task ValidateExamDtoAsync(ExamCreateDto dto, bool isPublishing = false)
    {
        if (dto.PassMark <= 0)
            throw new Exception("Pass mark must be greater than 0.");

        if (dto.DurationMinutes.HasValue && dto.DurationMinutes.Value <= 0)
            throw new Exception("Duration must be a positive number.");

        if (dto.MaxAttempts.HasValue && dto.MaxAttempts.Value < 1)
            throw new Exception("Max attempts must be at least 1.");

        if (isPublishing && !dto.Questions.Any())
            throw new Exception("Exam cannot be published without questions.");

        if (dto.Questions.Any())
        {
            // Validate duplicates
            var duplicateIds = dto.Questions.GroupBy(q => q.QuestionVersionId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateIds.Any())
                throw new Exception("Duplicate question versions are not allowed in the same exam.");

            var questionIds = dto.Questions.Select(q => q.QuestionVersionId).ToList();
            var validQuestions = await _context.QuestionVersions
                .Where(v => questionIds.Contains(v.Id) && v.Status == QuestionVersionStatus.Approved)
                .ToListAsync();

            if (validQuestions.Count != questionIds.Count)
                throw new Exception("One or more questions are invalid, missing, or not in an Approved State.");

            // Calculate total score
            decimal totalScore = 0;
            if (dto.ScoringMode == ScoringMode.EqualMarks)
            {
                if (!dto.DefaultQuestionMark.HasValue || dto.DefaultQuestionMark.Value <= 0)
                    throw new Exception("Default Question Mark must be provided and > 0 for EqualMarks scoring.");
                totalScore = dto.DefaultQuestionMark.Value * dto.Questions.Count;
            }
            else
            {
                foreach (var q in dto.Questions)
                {
                    if (!q.CustomMark.HasValue || q.CustomMark.Value <= 0)
                        throw new Exception("Every question must have a valid Custom Mark (>0) in CustomMarks scoring mode.");
                    totalScore += q.CustomMark.Value;
                }
            }

            if (dto.PassMark > totalScore)
                throw new Exception($"Pass mark ({dto.PassMark}) cannot exceed total obtainable score ({totalScore}).");
        }
    }

    public async Task<Exam> CreateExamAsync(ExamCreateDto dto, Guid creatorId)
    {
        await ValidateExamDtoAsync(dto, false);

        var exam = new Exam
        {
            CreatorId = creatorId,
            Title = dto.Title,
            Description = dto.Description,
            Instructions = dto.Instructions,
            PassMark = dto.PassMark,
            ScoringMode = dto.ScoringMode,
            DefaultQuestionMark = dto.DefaultQuestionMark,
            DurationMinutes = dto.DurationMinutes,
            MaxAttempts = dto.MaxAttempts,
            ShuffleQuestions = dto.ShuffleQuestions,
            ShuffleOptions = dto.ShuffleOptions,
            Status = ExamStatus.Draft,
            ExamQuestions = dto.Questions.Select(q => new ExamQuestion
            {
                QuestionVersionId = q.QuestionVersionId,
                OrderIndex = q.OrderIndex,
                CustomMark = q.CustomMark
            }).ToList()
        };

        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();
        return exam;
    }

    public async Task<Exam> UpdateExamAsync(Guid examId, ExamCreateDto dto, Guid creatorId)
    {
        var exam = await _context.Exams
            .Include(e => e.ExamQuestions)
            .FirstOrDefaultAsync(e => e.Id == examId && e.CreatorId == creatorId);

        if (exam == null) throw new Exception("Exam not found or you don't have permission.");
        if (exam.Status == ExamStatus.Published) throw new Exception("Cannot edit published exams.");

        await ValidateExamDtoAsync(dto, false);

        exam.Title = dto.Title;
        exam.Description = dto.Description;
        exam.Instructions = dto.Instructions;
        exam.PassMark = dto.PassMark;
        exam.ScoringMode = dto.ScoringMode;
        exam.DefaultQuestionMark = dto.DefaultQuestionMark;
        exam.DurationMinutes = dto.DurationMinutes;
        exam.MaxAttempts = dto.MaxAttempts;
        exam.ShuffleQuestions = dto.ShuffleQuestions;
        exam.ShuffleOptions = dto.ShuffleOptions;

        // Sync questions
        _context.ExamQuestions.RemoveRange(exam.ExamQuestions);
        exam.ExamQuestions = dto.Questions.Select(q => new ExamQuestion
        {
            QuestionVersionId = q.QuestionVersionId,
            OrderIndex = q.OrderIndex,
            CustomMark = q.CustomMark
        }).ToList();

        await _context.SaveChangesAsync();
        return exam;
    }

    public async Task PublishExamAsync(Guid examId, Guid creatorId)
    {
        var exam = await _context.Exams
            .Include(e => e.ExamQuestions)
            .FirstOrDefaultAsync(e => e.Id == examId && e.CreatorId == creatorId);

        if (exam == null) throw new Exception("Exam not found or you don't have permission.");

        // Create a DTO mapping on the fly to validate publishing constraints
        var mockDto = new ExamCreateDto
        {
            PassMark = exam.PassMark,
            ScoringMode = exam.ScoringMode,
            DefaultQuestionMark = exam.DefaultQuestionMark,
            DurationMinutes = exam.DurationMinutes,
            MaxAttempts = exam.MaxAttempts,
            Questions = exam.ExamQuestions.Select(q => new ExamQuestionDto { 
                QuestionVersionId = q.QuestionVersionId, 
                CustomMark = q.CustomMark, 
                OrderIndex = q.OrderIndex 
            }).ToList()
        };

        await ValidateExamDtoAsync(mockDto, true);

        exam.Status = ExamStatus.Published;
        await _context.SaveChangesAsync();
    }

    public async Task<List<Exam>> GetExamsForCreatorAsync(Guid creatorId)
    {
        return await _context.Exams
            .Include(e => e.ExamQuestions)
            .Where(e => e.CreatorId == creatorId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<Exam?> GetExamByIdAsync(Guid examId, Guid creatorId)
    {
        return await _context.Exams
            .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.QuestionVersion)
            .FirstOrDefaultAsync(e => e.Id == examId && e.CreatorId == creatorId);
    }
}
