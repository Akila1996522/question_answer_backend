using question_answer.Domain.Enums;

namespace question_answer.Application.DTOs;

public class ExamCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public decimal PassMark { get; set; }
    public ScoringMode ScoringMode { get; set; }
    public decimal? DefaultQuestionMark { get; set; }
    public int? DurationMinutes { get; set; }
    public int? MaxAttempts { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }

    public List<ExamQuestionDto> Questions { get; set; } = new();
}

public class ExamQuestionDto
{
    public Guid QuestionVersionId { get; set; }
    public int OrderIndex { get; set; }
    public decimal? CustomMark { get; set; }
}
