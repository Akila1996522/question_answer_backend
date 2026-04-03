using question_answer.Application.DTOs;

namespace question_answer.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto);
    Task<bool> VerifyRecaptchaAsync(string token);
}
