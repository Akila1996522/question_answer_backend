using question_answer.Domain.Enums;

namespace question_answer.Application.DTOs;

public class StartAttemptDto
{
    public Guid ExamId { get; set; }
}

public class SubmitAnswerDto
{
    public Guid AttemptQuestionId { get; set; }
    public List<Guid> SelectedOptionIds { get; set; } = new();
}

public class ExamStatusDto
{
    public AttemptStatus Status { get; set; }
    public int AnsweredCount { get; set; }
    public int UnansweredCount { get; set; }
    public int TotalQuestions { get; set; }
    public decimal CurrentScore { get; set; }
    public double? RemainingSeconds { get; set; }
    public AttemptQuestionDto? CurrentQuestion { get; set; }
}

public class AttemptQuestionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public List<AttemptOptionDto> Options { get; set; } = new();
    public int Order { get; set; }
    public bool IsAnswered { get; set; }
    public bool? WasCorrect { get; set; } // Sent back immediately after submission
    public string? Explanation { get; set; } // Sent back explicitly after submission
}

public class AttemptOptionDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class FinishResultDto
{
    public decimal TotalScore { get; set; }
    public decimal PassMark { get; set; }
    public bool Passed { get; set; }
    public int Answered { get; set; }
    public int Unanswered { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public double TimeSpentSeconds { get; set; }
}
