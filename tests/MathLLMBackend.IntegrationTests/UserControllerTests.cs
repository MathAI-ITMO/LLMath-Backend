using System.Net;
using Xunit;
using FluentAssertions;

namespace MathLLMBackend.IntegrationTests;

public class UserControllerTests : BaseIntegrationTest
{
    public UserControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/user/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithAuth_ReturnsUserInfo()
    {
        await CreateAndLoginUserAsync();
        var response = await AuthenticatedGetAsync("/api/user/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(TestUser!.Email);
    }
}
