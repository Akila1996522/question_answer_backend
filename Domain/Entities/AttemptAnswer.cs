namespace question_answer.Domain.Entities;

public class AttemptAnswer : BaseEntity
{
    public Guid AttemptQuestionId { get; set; }
    public AttemptQuestion? AttemptQuestion { get; set; }

    public Guid SelectedOptionId { get; set; }
    // No direct navigation to QuestionOption since they cascade delete, we just track the ID firmly or map it manually 

    public bool IsCorrectEvaluation { get; set; }
}
