using System.Net;
using Xunit;
using System.Net.Http.Json;
using FluentAssertions;
using MathLLMBackend.Core.Services.LlmService;
using MathLLMBackend.Presentation.Dtos.Chats;
using Moq;

namespace MathLLMBackend.IntegrationTests;

public class ChatControllerTests : BaseIntegrationTest
{
    public ChatControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateChat_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new CreateChatRequestDto("Test Chat", null);
        var response = await Client.PostAsJsonAsync("/api/chat/create", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateChat_WithAuth_ReturnsOk()
    {
        await CreateAndLoginUserAsync();
        var request = new CreateChatRequestDto("Test Chat", null);
        var response = await AuthenticatedPostAsync("/api/chat/create", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test Chat");
    }

    [Fact]
    public async Task GetChats_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/chat/get");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetChats_WithAuth_ReturnsOk()
    {
        await CreateAndLoginUserAsync();
        var response = await AuthenticatedGetAsync("/api/chat/get");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetChatDetails_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/chat/get/00000000-0000-0000-0000-000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetChatDetails_WithNonExistentChat_ReturnsNotFound()
    {
        await CreateAndLoginUserAsync();
        var response = await AuthenticatedGetAsync("/api/chat/get/00000000-0000-0000-0000-000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteChat_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync("/api/chat/delete/00000000-0000-0000-0000-000000000000", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteChat_WithNonExistentChat_ReturnsNotFound()
    {
        await CreateAndLoginUserAsync();
        var response = await AuthenticatedPostAsync("/api/chat/delete/00000000-0000-0000-0000-000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
