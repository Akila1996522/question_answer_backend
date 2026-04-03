namespace question_answer.Application.DTOs;

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RecaptchaToken { get; set; } = string.Empty;
}
