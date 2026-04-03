using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.EntityFrameworkCore;
using question_answer.Application.DTOs;
using question_answer.Application.Services;
using question_answer.Domain.Entities;
using question_answer.Domain.Enums;
using question_answer.Infrastructure.Data;
using Xunit;

namespace question_answer.UnitTests.Application.Services;

public class AuthServiceTests
{
    private readonly Mock<AppDbContext> _dbContextMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IDataProtectionProvider> _providerMock;
    private readonly Mock<IDataProtector> _protectorMock;
    private readonly Mock<IEmailService> _emailServiceMock;

    public AuthServiceTests()
    {
        _dbContextMock = new Mock<AppDbContext>(new Microsoft.EntityFrameworkCore.DbContextOptions<AppDbContext>());
        _configMock = new Mock<IConfiguration>();
        _providerMock = new Mock<IDataProtectionProvider>();
        _protectorMock = new Mock<IDataProtector>();
        _emailServiceMock = new Mock<IEmailService>();

        _providerMock.Setup(p => p.CreateProtector(It.IsAny<string>())).Returns(_protectorMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_PasswordsDoNotMatch_ReturnsError()
    {
        // Arrange
        var service = new AuthService(_dbContextMock.Object, _configMock.Object, _providerMock.Object, _emailServiceMock.Object);
        var dto = new RegisterDto { Password = "Password1", ConfirmPassword = "Password2" };

        // Act
        var result = await service.RegisterAsync(dto);

        // Assert
        Assert.Equal("Passwords do not match.", result.Message);
    }

    [Fact]
    public async Task ForgotPasswordAsync_UserExists_SendsEmail()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", FirstName = "John" };
        _dbContextMock.Setup(x => x.Users).ReturnsDbSet(new List<User> { user });

        _configMock.Setup(c => c["FRONTEND_BASE_URL"]).Returns("http://localhost:3000");
        _protectorMock.Setup(p => p.Protect(It.IsAny<byte[]>())).Returns(System.Text.Encoding.UTF8.GetBytes("mockedToken"));

        var service = new AuthService(_dbContextMock.Object, _configMock.Object, _providerMock.Object, _emailServiceMock.Object);
        
        // Act
        var result = await service.ForgotPasswordAsync(new ForgotPasswordDto { Email = "test@example.com" });

        // Assert
        Assert.Equal("If an account with that email exists, a reset link has been sent.", result.Message);
        
        // Verify email was actually "sent" via the interface mock exactly once
        _emailServiceMock.Verify(e => e.SendEmailAsync("test@example.com", "Secure Password Reset", It.IsAny<string>(), true), Times.Once);
    }
}
