namespace question_answer.Domain.Enums;

public enum UserStatus
{
    PendingEmailVerification = 0,
    PendingApproval = 1,
    Active = 2,
    Denied = 3,
    Blocked = 4
}
