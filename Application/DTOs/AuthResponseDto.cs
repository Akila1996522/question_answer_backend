using question_answer.Domain.Enums;

namespace question_answer.Application.DTOs;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}
