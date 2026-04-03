using Microsoft.EntityFrameworkCore;
using question_answer.Application.DTOs;
using question_answer.Domain.Entities;
using question_answer.Domain.Enums;
using question_answer.Infrastructure.Data;

namespace question_answer.Application.Services;

public class ExamTakingService : IExamTakingService
{
    private readonly AppDbContext _context;

    public ExamTakingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ExamAttempt> StartAttemptAsync(StartAttemptDto dto, Guid userId)
    {
        var exam = await _context.Exams
            .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.QuestionVersion)
                    .ThenInclude(qv => qv.Options)
            .FirstOrDefaultAsync(e => e.Id == dto.ExamId);

        if (exam == null || exam.Status != ExamStatus.Published)
            throw new Exception("Exam not found or is not published.");

        var attemptsCount = await _context.ExamAttempts
            .CountAsync(a => a.ExamId == exam.Id && a.UserId == userId);

        if (exam.MaxAttempts.HasValue && attemptsCount >= exam.MaxAttempts.Value)
            throw new Exception("You have reached the maximum number of attempts for this exam.");

        // Create new attempt
        var now = DateTime.UtcNow;
        var attempt = new ExamAttempt
        {
            ExamId = exam.Id,
            UserId = userId,
            StartedAt = now,
            ExpiresAt = exam.DurationMinutes.HasValue ? now.AddMinutes(exam.DurationMinutes.Value) : null,
            Status = AttemptStatus.InProgress,
            AttemptNumber = attemptsCount + 1,
            Score = 0,
            PassMarkSnapshot = exam.PassMark,
            Passed = false
        };

        var questionsList = exam.ExamQuestions.ToList();
        
        // Snapshot logic: If Shuffle questions requested, randomize here
        if (exam.ShuffleQuestions)
        {
            var random = new Random();
            questionsList = questionsList.OrderBy(q => random.Next()).ToList();
        }
        else
        {
            questionsList = questionsList.OrderBy(q => q.OrderIndex).ToList();
        }

        int renderIdx = 1;
        foreach(var eq in questionsList)
        {
            var optionsList = eq.QuestionVersion.Options.Select(o => o.Id).ToList();
            if (exam.ShuffleOptions)
            {
                var random = new Random();
                optionsList = optionsList.OrderBy(o => random.Next()).ToList();
            }

            // For equal marks calculation dynamically if it was missing 
            decimal assignedMark = exam.ScoringMode == ScoringMode.EqualMarks 
                 ? (exam.DefaultQuestionMark ?? 1) 
                 : (eq.CustomMark ?? 0);

            var attemptQuestion = new AttemptQuestion
            {
                QuestionVersionId = eq.QuestionVersionId,
                RenderedOrder = renderIdx++,
                RenderedOptionOrderIds = string.Join(",", optionsList),
                AssignedMark = assignedMark,
                IsAnswered = false
            };
            attempt.AttemptQuestions.Add(attemptQuestion);
        }

        _context.ExamAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return attempt;
    }

    private async Task EnforceTimeLimit(ExamAttempt attempt)
    {
        if (attempt.Status == AttemptStatus.InProgress && attempt.ExpiresAt.HasValue && attempt.ExpiresAt.Value <= DateTime.UtcNow)
        {
            // Auto-submit
            attempt.Status = AttemptStatus.TimedOut;
            attempt.SubmittedAt = attempt.ExpiresAt; // Record exactly at expiry mark
            
            // finalize scoring
            FinalizeScoring(attempt);
            await _context.SaveChangesAsync();
            throw new Exception("Time limit reached. Your exam has been automatically submitted.");
        }
    }

    private void FinalizeScoring(ExamAttempt attempt)
    {
        decimal totalScore = 0;
        foreach (var aq in attempt.AttemptQuestions)
        {
            if (aq.IsAnswered)
            {
                var answer = aq.Answers.FirstOrDefault();
                if (answer != null && answer.IsCorrectEvaluation)
                {
                    totalScore += aq.AssignedMark;
                }
            }
        }
        attempt.Score = totalScore;
        attempt.Passed = totalScore >= attempt.PassMarkSnapshot;
    }

    public async Task<AttemptQuestionDto> SubmitAnswerAsync(Guid attemptId, SubmitAnswerDto dto, Guid userId)
    {
        var attempt = await _context.ExamAttempts
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.Answers)
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.QuestionVersion)
                    .ThenInclude(qv => qv.Options)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId);

        if (attempt == null) throw new Exception("Attempt not found access denied.");
        if (attempt.Status != AttemptStatus.InProgress) throw new Exception("This attempt is no longer active.");

        await EnforceTimeLimit(attempt);

        var aq = attempt.AttemptQuestions.FirstOrDefault(q => q.Id == dto.AttemptQuestionId);
        if (aq == null) throw new Exception("Question not found in this attempt.");
        
        if (aq.IsAnswered) throw new Exception("You have already answered this question.");

        // Check if selected options are correct against QuestionVersion map
        // Currently supporting simple multi-select evaluation (ALL selected options must be exactly the set of correct options, no partial points)
        var correctOptionIds = aq.QuestionVersion.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
        
        bool isCorrect = true;
        if (correctOptionIds.Count != dto.SelectedOptionIds.Count) {
            isCorrect = false;
        } else {
            foreach(var id in dto.SelectedOptionIds) {
                if(!correctOptionIds.Contains(id)) isCorrect = false;
            }
        }

        // Persist answers
        foreach(var selectedId in dto.SelectedOptionIds)
        {
            aq.Answers.Add(new AttemptAnswer
            {
                SelectedOptionId = selectedId,
                IsCorrectEvaluation = isCorrect 
            });
        }
        
        aq.IsAnswered = true;
        
        // Update live score loosely
        if(isCorrect) attempt.Score += aq.AssignedMark; 

        await _context.SaveChangesAsync();

        // One-question-at-a-time immediate feedback
        return new AttemptQuestionDto
        {
            Id = aq.Id,
            Text = aq.QuestionVersion.Text,
            IsAnswered = true,
            WasCorrect = isCorrect,
            Explanation = aq.QuestionVersion.Explanation
        };
    }

    public async Task<FinishResultDto> FinishAttemptAsync(Guid attemptId, Guid userId)
    {
        var attempt = await _context.ExamAttempts
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId);

        if (attempt == null) throw new Exception("Attempt not found");
        
        if (attempt.Status == AttemptStatus.InProgress)
        {
            // If manual finish
            attempt.Status = AttemptStatus.Submitted;
            attempt.SubmittedAt = DateTime.UtcNow;
            FinalizeScoring(attempt);
            await _context.SaveChangesAsync();
        }

        int answeredCount = attempt.AttemptQuestions.Count(aq => aq.IsAnswered);
        
        return new FinishResultDto
        {
            TotalScore = attempt.Score,
            PassMark = attempt.PassMarkSnapshot,
            Passed = attempt.Passed,
            Answered = answeredCount,
            Unanswered = attempt.AttemptQuestions.Count - answeredCount,
            StartedAt = attempt.StartedAt,
            SubmittedAt = attempt.SubmittedAt,
            TimeSpentSeconds = attempt.SubmittedAt.HasValue 
                ? (attempt.SubmittedAt.Value - attempt.StartedAt).TotalSeconds 
                : 0
        };
    }

    public async Task<ExamStatusDto> GetAttemptStatusAsync(Guid attemptId, Guid userId)
    {
        var attempt = await _context.ExamAttempts
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.QuestionVersion)
                    .ThenInclude(qv => qv.Options)
            .Include(a => a.AttemptQuestions)
                .ThenInclude(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId);

        if (attempt == null) throw new Exception("Attempt not found.");

        if (attempt.Status == AttemptStatus.InProgress)
        {
            try { await EnforceTimeLimit(attempt); } catch { /* Handled, status updated */ }
        }

        var unanswered = attempt.AttemptQuestions.Where(aq => !aq.IsAnswered).OrderBy(aq => aq.RenderedOrder).ToList();
        var currentQuestion = unanswered.FirstOrDefault();

        AttemptQuestionDto? currentDto = null;
        if (currentQuestion != null)
        {
             // Rebuild option payload mapping using snapshot order
             var optionIds = currentQuestion.RenderedOptionOrderIds.Split(',').Select(Guid.Parse).ToList();
             var optionDtos = new List<AttemptOptionDto>();
             
             foreach(var oid in optionIds)
             {
                 var optEntity = currentQuestion.QuestionVersion.Options.FirstOrDefault(o => o.Id == oid);
                 if (optEntity != null) {
                     optionDtos.Add(new AttemptOptionDto {
                         Id = optEntity.Id,
                         Text = optEntity.Text
                     });
                 }
             }

             currentDto = new AttemptQuestionDto
             {
                 Id = currentQuestion.Id,
                 Text = currentQuestion.QuestionVersion.Text,
                 Type = currentQuestion.QuestionVersion.Type,
                 Order = currentQuestion.RenderedOrder,
                 IsAnswered = false,
                 Options = optionDtos
             };
        }

        int answeredCount = attempt.AttemptQuestions.Count(aq => aq.IsAnswered);
        double? remainingSeconds = null;

        if (attempt.Status == AttemptStatus.InProgress && attempt.ExpiresAt.HasValue)
        {
            remainingSeconds = (attempt.ExpiresAt.Value - DateTime.UtcNow).TotalSeconds;
            if (remainingSeconds < 0) remainingSeconds = 0;
        }

        return new ExamStatusDto
        {
            Status = attempt.Status,
            AnsweredCount = answeredCount,
            UnansweredCount = attempt.AttemptQuestions.Count - answeredCount,
            TotalQuestions = attempt.AttemptQuestions.Count,
            CurrentScore = attempt.Score,
            RemainingSeconds = remainingSeconds,
            CurrentQuestion = currentDto
        };
    }
}
