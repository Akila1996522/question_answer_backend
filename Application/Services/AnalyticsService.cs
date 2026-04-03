using Microsoft.EntityFrameworkCore;
using question_answer.Application.DTOs;
using question_answer.Domain.Enums;
using question_answer.Infrastructure.Data;

namespace question_answer.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _context;

    public AnalyticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SuperAdminAnalyticsDto> GetSuperAdminAnalyticsAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var pendingApprovals = await _context.QuestionVersions.CountAsync(q => q.Status == QuestionVersionStatus.PendingApproval);
        var totalExams = await _context.Exams.CountAsync();
        var allAttempts = await _context.ExamAttempts.Where(a => a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.TimedOut).ToListAsync();

        var passRate = allAttempts.Any() 
            ? Math.Round((decimal)allAttempts.Count(a => a.Passed) / allAttempts.Count * 100, 2) 
            : 0;

        // Recent activity from Audit Logs
        var recentLogs = await _context.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();

        var recentActivities = recentLogs.Select(l => new RecentActivityDto {
            Timestamp = l.CreatedAt,
            Description = $"{l.Action} on {l.EntityName}{(l.EntityId != null ? " (" + l.EntityId + ")" : "")} by User {l.UserId}"
        }).ToList();

        // Question performance
        var questions = await _context.AttemptQuestions
            .Include(aq => aq.QuestionVersion)
            .Include(aq => aq.Answers)
            .ToListAsync();

        var groupedQ = questions.GroupBy(q => q.QuestionVersionId).Select(g => new QuestionPerformanceDto {
            QuestionExcerpt = g.First().QuestionVersion?.Text.Substring(0, Math.Min(g.First().QuestionVersion!.Text.Length, 30)) ?? "N/A",
            IncludedInExamsCount = g.Count(),
            CorrectnessRate = g.Any(aq => aq.IsAnswered) 
                ? Math.Round((decimal)g.Count(aq => aq.Answers.Any() && aq.Answers.First().IsCorrectEvaluation) / g.Count(aq => aq.IsAnswered) * 100, 2) 
                : 0
        }).OrderByDescending(g => g.IncludedInExamsCount).Take(5).ToList();

        return new SuperAdminAnalyticsDto
        {
            TotalUsers = totalUsers,
            PendingApprovalsCount = pendingApprovals,
            TotalExams = totalExams,
            TotalAttempts = allAttempts.Count,
            OverallPassRate = passRate,
            RecentActivity = recentActivities,
            TopQuestions = groupedQ
        };
    }

    public async Task<CreatorAnalyticsDto> GetCreatorAnalyticsAsync(Guid creatorId)
    {
        var myQuestionsCount = await _context.Questions.CountAsync(q => q.OriginalCreatorId == creatorId);
        var myExams = await _context.Exams.Where(e => e.CreatorId == creatorId).ToListAsync();
        
        var myExamIds = myExams.Select(e => e.Id).ToList();
        var attemptsOnMyExams = await _context.ExamAttempts
             .Where(a => myExamIds.Contains(a.ExamId) && (a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.TimedOut))
             .ToListAsync();

        var performances = myExams.Select(e => {
            var examAttempts = attemptsOnMyExams.Where(a => a.ExamId == e.Id).ToList();
            return new ExamPerformanceDto {
                ExamTitle = e.Title,
                TotalAttempts = examAttempts.Count,
                PassCount = examAttempts.Count(a => a.Passed),
                FailCount = examAttempts.Count(a => !a.Passed),
                AverageScore = examAttempts.Any() ? Math.Round(examAttempts.Average(a => a.Score), 2) : 0
            };
        }).ToList();

        return new CreatorAnalyticsDto
        {
            MyQuestionsCount = myQuestionsCount,
            MyExamsCount = myExams.Count,
            TotalAttemptsOnMyExams = attemptsOnMyExams.Count,
            ExamPerformances = performances
        };
    }
}
