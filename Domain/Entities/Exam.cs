using question_answer.Domain.Enums;

namespace question_answer.Domain.Entities;

public class Exam : BaseEntity
{
    public Guid CreatorId { get; set; }
    public User? Creator { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }

    // Scoring Rules
    public decimal PassMark { get; set; }
    public ScoringMode ScoringMode { get; set; } = ScoringMode.EqualMarks;
    public decimal? DefaultQuestionMark { get; set; } // Used if ScoringMode is EqualMarks

    // Settings
    public int? DurationMinutes { get; set; }
    public int? MaxAttempts { get; set; } // If null, infinite attempts. If set, >= 1
    
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }

    public ExamStatus Status { get; set; } = ExamStatus.Draft;

    // Relationships
    public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
}
