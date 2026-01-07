using MathLLMBackend.Core.Constants;
using MathLLMBackend.Core.Services.ChatService;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using MathLLMBackend.Domain.Exceptions;
using MathLLMBackend.Presentation.Binders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MathLLMBackend.Presentation.Dtos.Chats;

namespace MathLLMBackend.Presentation.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateChat([FromBody] CreateChatRequestDto dto, [FromUserId] string userId, CancellationToken ct)
        {
            var chat = new Chat(dto.Name, userId);
            var createdChat = dto.ProblemHash == null
                ? await _chatService.Create(chat, ct)
                : await _chatService.Create(chat, dto.ProblemHash, TaskTypes.Default, ct);
            
            return Ok(
                new ChatDto(createdChat.Id, createdChat.Name, createdChat.Type?.ToString() ?? ChatConstants.DefaultChatTypeName, null, null)
            );
            
        }
        
        [HttpGet("get")]
        public async Task<IActionResult> GetChats([FromUserId] string userId, CancellationToken ct)
        {
            var chats = await _chatService.GetUserChats(userId, ct);
            return Ok(chats.Select(c => new ChatDto(c.Id, c.Name, c.Type?.ToString() ?? ChatConstants.DefaultChatTypeName, null, null)).ToList());
        }

        [HttpGet("get/{chatId:guid}")]
        public async Task<IActionResult> GetChatDetails(Guid chatId, CancellationToken ct)
        {
            var chat = await _chatService.GetChatById(chatId, ct);
            if (chat == null)
            {
                _logger.LogWarning("Chat with ID {ChatId} not found when trying to get details.", chatId);
                return NotFound();
            }

            var details = await _chatService.GetChatDetailsAsync(chatId, ct);
            return Ok(new ChatDto(chat.Id, chat.Name, chat.Type?.ToString() ?? ChatConstants.DefaultChatTypeName, details.TaskType, details.TheoryLink));
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> DeleteChat(Guid id, [FromUserId] string userId, CancellationToken ct)
        {
            await _chatService.DeleteChat(id, userId, ct);
            return Ok();
        }
    }
}
