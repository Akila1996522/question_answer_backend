namespace question_answer.Domain.Entities;

public class ExamQuestion : BaseEntity
{
    public Guid ExamId { get; set; }
    public Exam? Exam { get; set; }

    // Hard linked to a specific version, not the parent abstract question
    public Guid QuestionVersionId { get; set; }
    public QuestionVersion? QuestionVersion { get; set; }

    // Ordering sequence for the exam
    public int OrderIndex { get; set; }

    // Required if Exam uses CustomMarks, otherwise ignored or auto-populated via DefaultQuestionMark
    public decimal? CustomMark { get; set; }
}
