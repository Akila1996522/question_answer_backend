using question_answer.Domain.Enums;

namespace question_answer.Domain.Entities;

public class QuestionVersion : BaseEntity
{
    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }

    public int VersionNumber { get; set; }
    public QuestionVersionStatus Status { get; set; } = QuestionVersionStatus.Draft;
    public QuestionType Type { get; set; }

    public string Text { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? References { get; set; }

    // Relationships
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();

    // Audit fields for approval workflow
    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}
