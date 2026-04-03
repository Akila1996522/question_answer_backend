namespace question_answer.Domain.Entities;

public class AttemptQuestion : BaseEntity
{
    public Guid ExamAttemptId { get; set; }
    public ExamAttempt? ExamAttempt { get; set; }

    public Guid QuestionVersionId { get; set; }
    public QuestionVersion? QuestionVersion { get; set; }

    public int RenderedOrder { get; set; }
    
    public decimal AssignedMark { get; set; }

    // To preserve shuffle state across refreshes, store comma-separated list of Option IDs in randomized order
    public string RenderedOptionOrderIds { get; set; } = string.Empty;

    // A flag allowing the engine to explicitly block re-submissions if answering is final
    public bool IsAnswered { get; set; }

    // Relationship
    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
}
