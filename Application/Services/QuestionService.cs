using Microsoft.EntityFrameworkCore;
using question_answer.Application.DTOs;
using question_answer.Domain.Entities;
using question_answer.Domain.Enums;
using question_answer.Infrastructure.Data;

namespace question_answer.Application.Services;

public class QuestionService : IQuestionService
{
    private readonly AppDbContext _context;

    public QuestionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Question> CreateQuestionDraftAsync(ImportedQuestionDto dto, Guid creatorId)
    {
        var question = new Question
        {
            OriginalCreatorId = creatorId
        };
        _context.Questions.Add(question);

        var version = new QuestionVersion
        {
            QuestionId = question.Id,
            VersionNumber = 1,
            Status = QuestionVersionStatus.Draft,
            Type = dto.Type,
            Text = dto.Text,
            Explanation = dto.Explanation,
            References = dto.Reference,
            Options = dto.Options.Select(o => new QuestionOption
            {
                Text = o.Text,
                IsCorrect = o.IsCorrect
            }).ToList()
        };
        
        _context.QuestionVersions.Add(version);
        await _context.SaveChangesAsync();

        return question;
    }

    public async Task<List<Question>> GetQuestionsForCreatorAsync(Guid creatorId)
    {
        return await _context.Questions
            .Include(q => q.Versions)
                .ThenInclude(v => v.Options)
            .Where(q => q.OriginalCreatorId == creatorId)
            .ToListAsync();
    }

    public async Task<QuestionVersion> EditQuestionVersionAsync(Guid questionId, ImportedQuestionDto dto, Guid creatorId)
    {
        var question = await _context.Questions
            .Include(q => q.Versions)
                .ThenInclude(v => v.Options)
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question == null) throw new Exception("Question not found");

        var latestVersion = question.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (latestVersion == null) throw new Exception("No versions exist");

        // If the latest version is already Approved, we must spawn a new version
        if (latestVersion.Status == QuestionVersionStatus.Approved)
        {
            var newVersion = new QuestionVersion
            {
                QuestionId = question.Id,
                VersionNumber = latestVersion.VersionNumber + 1,
                Status = QuestionVersionStatus.PendingApproval, // Usually goes to pending or draft
                Type = dto.Type,
                Text = dto.Text,
                Explanation = dto.Explanation,
                References = dto.Reference,
                Options = dto.Options.Select(o => new QuestionOption
                {
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).ToList()
            };
            _context.QuestionVersions.Add(newVersion);
            await _context.SaveChangesAsync();
            return newVersion;
        }

        // Otherwise modify in-place for Draft/Pending/Rejected
        latestVersion.Text = dto.Text;
        latestVersion.Explanation = dto.Explanation;
        latestVersion.References = dto.Reference;
        latestVersion.Type = dto.Type;
        latestVersion.Status = QuestionVersionStatus.PendingApproval; // Move back to pending on edit
        
        // Update options (simple approach: delete old, create new)
        _context.QuestionOptions.RemoveRange(latestVersion.Options);
        latestVersion.Options = dto.Options.Select(o => new QuestionOption
        {
            Text = o.Text,
            IsCorrect = o.IsCorrect
        }).ToList();

        await _context.SaveChangesAsync();
        return latestVersion;
    }

    public async Task<List<QuestionVersion>> GetPendingApprovalsAsync()
    {
        return await _context.QuestionVersions
            .Include(v => v.Question)
                .ThenInclude(q => q.OriginalCreator)
            .Include(v => v.Options)
            .Where(v => v.Status == QuestionVersionStatus.PendingApproval)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync();
    }

    public async Task<QuestionVersion> ApproveQuestionAsync(Guid questionVersionId, Guid approverId)
    {
        var version = await _context.QuestionVersions.FindAsync(questionVersionId);
        if (version == null) throw new Exception("Version not found");

        version.Status = QuestionVersionStatus.Approved;
        version.ApprovedById = approverId;
        version.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return version;
    }

    public async Task<QuestionVersion> RejectQuestionAsync(Guid questionVersionId, Guid approverId, string reason)
    {
        var version = await _context.QuestionVersions.FindAsync(questionVersionId);
        if (version == null) throw new Exception("Version not found");

        version.Status = QuestionVersionStatus.Rejected;
        version.RejectionReason = reason;

        await _context.SaveChangesAsync();
        return version;
    }
}
