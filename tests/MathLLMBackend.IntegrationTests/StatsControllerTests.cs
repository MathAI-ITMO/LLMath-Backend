using System.Net;
using Xunit;
using FluentAssertions;
using System.Net.Http.Json;

namespace MathLLMBackend.IntegrationTests;

[Collection("Integration Tests")]
public class StatsControllerTests : BaseIntegrationTest
{
    public StatsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetTaskModeTitles_ReturnsOk()
    {
        var response = await AuthenticatedGetAsync("/api/stats/task-mode-titles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetUserStats_ReturnsOk()
    {
        var response = await AuthenticatedGetAsync("/api/stats/user-stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserDetails_WithValidUserId_ReturnsOk()
    {
        var user = await CreateAndLoginUserAsync();
        var response = await AuthenticatedClient!.GetAsync($"/api/stats/user-details/{user.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserDetails_WithOtherUserId_ReturnsForbidden()
    {
        await CreateAndLoginUserAsync();
        var response = await AuthenticatedClient!.GetAsync("/api/stats/user-details/some-other-user-id");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
