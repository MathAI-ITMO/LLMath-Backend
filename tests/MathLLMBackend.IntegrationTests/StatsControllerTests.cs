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
        var response = await Client.GetAsync("/api/stats/task-mode-titles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetUserStats_ReturnsOk()
    {
        await CreateAndLoginUserAsync();
        var response = await Client.GetAsync("/api/stats/user-stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserDetails_WithValidUserId_ReturnsOk()
    {
        var user = await CreateAndLoginUserAsync();
        var response = await Client.GetAsync($"/api/stats/user-details/{user.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserDetails_WithInvalidUserId_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/stats/user-details/invalid-user-id");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        content.Should().NotBeNull();
    }
}
