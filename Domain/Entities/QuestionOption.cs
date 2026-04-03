namespace question_answer.Domain.Entities;

public class QuestionOption : BaseEntity
{
    public Guid QuestionVersionId { get; set; }
    public QuestionVersion? QuestionVersion { get; set; }

    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
