using FastBiteGroupMCA.API;
using FastBiteGroupMCA.Application.DTOs.Auth;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestFastBiteGroupMCA.IntegrationTests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "testuser@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Test",
            LastName = "User",
            DateOfBirth = new System.DateTime(2000, 1, 1)
        };
        var content = new StringContent(JsonConvert.SerializeObject(registerDto), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var stringResponse = await response.Content.ReadAsStringAsync();
        Assert.Contains("Đăng ký thành công", stringResponse);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        // First, register a user to ensure the user exists for login
        var registerDto = new RegisterDto
        {
            Email = "loginuser@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Login",
            LastName = "User",
            DateOfBirth = new System.DateTime(2000, 1, 1)
        };
        var registerContent = new StringContent(JsonConvert.SerializeObject(registerDto), Encoding.UTF8, "application/json");
        await _client.PostAsync("/api/auth/register", registerContent);

        // Now, attempt to log in
        var loginDto = new LoginDto
        {
            Email = "loginuser@example.com",
            Password = "Password123!"
        };
        var loginContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", loginContent);

        // Assert
        response.EnsureSuccessStatusCode();
        var stringResponse = await response.Content.ReadAsStringAsync();
        dynamic result = JsonConvert.DeserializeObject(stringResponse);
        Assert.NotNull(result.data.accessToken);
        Assert.NotNull(result.data.refreshToken);
    }
}
