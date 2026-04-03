using Microsoft.AspNetCore.Mvc;
using Moq;
using question_answer.API.Controllers;
using question_answer.Application.DTOs;
using question_answer.Application.Services;
using Xunit;

namespace question_answer.UnitTests.API.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task ForgotPassword_ReturnsOk_WithGenericMessage()
    {
        // Arrange
        var dto = new ForgotPasswordDto { Email = "test@example.com" };
        var expectedResponse = new AuthResponseDto { Message = "If an account with that email exists, a reset link has been sent." };
        _authServiceMock.Setup(s => s.ForgotPasswordAsync(dto)).ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ForgotPassword(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal(expectedResponse.Message, returnValue.Message);
    }
}
