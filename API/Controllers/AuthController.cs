using Microsoft.AspNetCore.Mvc;
using question_answer.Application.DTOs;
using question_answer.Application.Services;

namespace question_answer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var response = await _authService.RegisterAsync(dto);
        if (string.IsNullOrEmpty(response.Message) || response.Message == "Passwords do not match." || response.Message == "Email is already registered." || response.Message == "Invalid ReCAPTCHA.")
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var response = await _authService.LoginAsync(dto);
        if (string.IsNullOrEmpty(response.Token))
            return Unauthorized(response);

        return Ok(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var response = await _authService.ForgotPasswordAsync(dto);
        return Ok(response);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var response = await _authService.ResetPasswordAsync(dto);
        if (response.Message == "Password reset successful.")
            return Ok(response);

        return BadRequest(response);
    }
}
