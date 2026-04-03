using question_answer.Domain.Enums;

namespace question_answer.Domain.Entities;

public class ExamAttempt : BaseEntity
{
    public Guid ExamId { get; set; }
    public Exam? Exam { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;
    
    public int AttemptNumber { get; set; }
    
    public decimal Score { get; set; }
    public decimal PassMarkSnapshot { get; set; }
    public bool Passed { get; set; }

    // Relationships
    public ICollection<AttemptQuestion> AttemptQuestions { get; set; } = new List<AttemptQuestion>();
}
