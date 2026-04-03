namespace question_answer.Domain.Entities;

public class Question : BaseEntity
{
    // Represents a stable identity for an evolving question
    public Guid OriginalCreatorId { get; set; }
    public User? OriginalCreator { get; set; }

    public ICollection<QuestionVersion> Versions { get; set; } = new List<QuestionVersion>();
}
