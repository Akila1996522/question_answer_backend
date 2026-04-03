using question_answer.Domain.Enums;

namespace question_answer.Domain.Entities;

public class Role : BaseEntity
{
    public RoleType Name { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
