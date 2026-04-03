using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using question_answer.Application.DTOs;
using question_answer.Domain.Entities;
using question_answer.Domain.Enums;
using question_answer.Infrastructure.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace question_answer.Application.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IDataProtector _protector;
    private readonly IEmailService _emailService;
    private static readonly HttpClient _httpClient = new HttpClient();

    public AuthService(AppDbContext context, IConfiguration config, IDataProtectionProvider provider, IEmailService emailService)
    {
        _context = context;
        _config = config;
        _protector = provider.CreateProtector("PasswordReset");
        _emailService = emailService;
    }

    public async Task<bool> VerifyRecaptchaAsync(string token)
    {
        var secretKey = _config["Recaptcha:SecretKey"];
        var isDev = _config["ASPNETCORE_ENVIRONMENT"] == "Development";
        var bypassInDev = _config.GetValue<bool>("Recaptcha:BypassInDevelopment");

        if (isDev && bypassInDev && token == "dev-token-bypass")
            return true;

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(secretKey))
            return false;

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("secret", secretKey),
            new KeyValuePair<string, string>("response", token)
        });

        var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
        if (!response.IsSuccessStatusCode)
            return false;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("success").GetBoolean();
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
        {
            return new AuthResponseDto { Message = "Passwords do not match." };
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
        {
            return new AuthResponseDto { Message = "Email is already registered." };
        }

        if (!await VerifyRecaptchaAsync(dto.RecaptchaToken))
        {
            return new AuthResponseDto { Message = "Invalid ReCAPTCHA." };
        }

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Status = UserStatus.PendingEmailVerification
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Status = user.Status,
            Message = "Registration successful. Please verify your email."
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        if (!await VerifyRecaptchaAsync(dto.RecaptchaToken))
        {
            return new AuthResponseDto { Message = "Invalid ReCAPTCHA." };
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return new AuthResponseDto { Message = "Invalid email or password." };
        }

        if (user.Status == UserStatus.PendingEmailVerification)
            return new AuthResponseDto { Status = user.Status, Message = "Email not verified." };
        
        if (user.Status == UserStatus.PendingApproval)
            return new AuthResponseDto { Status = user.Status, Message = "Account is not activated." };
        
        if (user.Status == UserStatus.Denied || user.Status == UserStatus.Blocked)
            return new AuthResponseDto { Status = user.Status, Message = "Access denied." };

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Status = user.Status,
            Message = "Login successful."
        };
    }

    public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        
        if (user != null)
        {
            var tokenPayload = $"{user.Id}:{DateTime.UtcNow.AddHours(24).Ticks}";
            var resetToken = _protector.Protect(tokenPayload);
            
            var frontendUrl = _config["FRONTEND_BASE_URL"] ?? Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:3000";
            var encodedEmail = System.Web.HttpUtility.UrlEncode(user.Email);
            var encodedToken = System.Web.HttpUtility.UrlEncode(resetToken);
            
            var resetLink = $"{frontendUrl}/reset-password?email={encodedEmail}&token={encodedToken}";
            
            var emailBody = $@"
                <h3>Password Reset Request</h3>
                <p>Hello {user.FirstName},</p>
                <p>We received a request to reset your password. Click the link below to set a new password:</p>
                <p><a href='{resetLink}'>Reset Password</a></p>
                <p>If you did not request this, please ignore this email.</p>";

            await _emailService.SendEmailAsync(user.Email, "Secure Password Reset", emailBody, true);
        }

        return new AuthResponseDto { Message = "If an account with that email exists, a reset link has been sent." };
    }

    public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            return new AuthResponseDto { Message = "Invalid request." };
        }

        try
        {
            var unprotected = _protector.Unprotect(dto.Token);
            var parts = unprotected.Split(':');
            if (parts.Length == 2 && Guid.TryParse(parts[0], out var userId) && long.TryParse(parts[1], out var ticks))
            {
                if (userId != user.Id) return new AuthResponseDto { Message = "Invalid token." };
                if (new DateTime(ticks) < DateTime.UtcNow) return new AuthResponseDto { Message = "Token expired." };

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await _context.SaveChangesAsync();
                
                return new AuthResponseDto { Message = "Password reset successful." };
            }
        }
        catch
        {
            // Invalid/corrupted token
        }

        return new AuthResponseDto { Message = "Invalid token." };
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
        };

        foreach (var userRole in user.UserRoles)
        {
            if (userRole.Role != null)
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "super_secret_fallback_key_for_dev_mode_only_32_bytes"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "QuestionAnswerApi",
            audience: _config["Jwt:Audience"] ?? "QuestionAnswerApp",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
