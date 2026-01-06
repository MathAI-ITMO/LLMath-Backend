using System.Net;
using Xunit;
using System.Net.Http.Json;
using FluentAssertions;
using MathLLMBackend.Core.Services.ChatService;
using MathLLMBackend.Core.Services.LlmService;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using MathLLMBackend.Presentation.Dtos.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace MathLLMBackend.IntegrationTests;

public class MessageControllerTests : BaseIntegrationTest
{
    public MessageControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Complete_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new MessageCreateDto(Guid.NewGuid(), "Test message");
        var response = await Client.PostAsJsonAsync("/api/message/complete", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Complete_WithValidChat_ReturnsOk()
    {
        await CreateAndLoginUserAsync();
        
        Factory.LlmServiceMock.Reset();
        Factory.LlmServiceMock
            .Setup(x => x.GenerateNextMessageAsync(It.IsAny<List<Message>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test response");
        
        using var scope = Factory.Services.CreateScope();
        var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
        var chat = new Chat("Test Chat", TestUser!.Id);
        var createdChat = await chatService.Create(chat, CancellationToken.None);
        createdChat.Id.Should().NotBe(Guid.Empty);

        var request = new MessageCreateDto(createdChat.Id, "Test message");
        var response = await AuthenticatedPostAsync("/api/message/complete", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllMessagesFromChat_WithValidChat_ReturnsOk()
    {
        await CreateAndLoginUserAsync();
        
        using var scope = Factory.Services.CreateScope();
        var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
        var chat = new Chat("Test Chat", TestUser!.Id);
        var createdChat = await chatService.Create(chat, CancellationToken.None);

        var response = await AuthenticatedGetAsync($"/api/message/get-messages-from-chat?chatId={createdChat.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllMessagesFromChat_WithNonExistentChat_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/api/message/get-messages-from-chat?chatId=00000000-0000-0000-0000-000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
