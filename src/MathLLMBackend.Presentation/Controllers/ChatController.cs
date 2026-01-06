using MathLLMBackend.Core.Services.ChatService;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MathLLMBackend.Presentation.Dtos.Chats;
using Microsoft.AspNetCore.Identity;

namespace MathLLMBackend.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(IChatService chatService, ILogger<ChatController> logger, UserManager<ApplicationUser> userManager)
        {
            _chatService = chatService;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateChat([FromBody] CreateChatRequestDto dto, CancellationToken ct)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
            {
                return Unauthorized();
            }
            
            var chat = new Chat(dto.Name, userId);
            var createdChat = dto.ProblemHash == null
                ? await _chatService.Create(chat, ct)
                : await _chatService.Create(chat, dto.ProblemHash, 0, ct);
            
            return Ok(
                new ChatDto(createdChat.Id, createdChat.Name, createdChat.Type?.ToString() ?? "Chat", null, null)
            );
            
        }
        
        [HttpGet("get")]
        [Authorize]
        public async Task<IActionResult> GetChats(CancellationToken ct)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
            {
                return Unauthorized();
            }
            
            var chats = await _chatService.GetUserChats(userId, ct);
            return Ok(chats.Select(c => new ChatDto(c.Id, c.Name, c.Type?.ToString() ?? "Chat", null, null)).ToList());
        }

        [HttpGet("get/{chatId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetChatDetails(Guid chatId, CancellationToken ct)
        {
            var chat = await _chatService.GetChatById(chatId, ct);
            if (chat == null)
            {
                _logger.LogWarning("Chat with ID {ChatId} not found when trying to get details.", chatId);
                return NotFound();
            }

            var details = await _chatService.GetChatDetailsAsync(chatId, ct);
            return Ok(new ChatDto(chat.Id, chat.Name, chat.Type?.ToString() ?? "Chat", details.TaskType, details.TheoryLink));
        }

        [HttpPost("delete/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteChat(Guid id, CancellationToken ct)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
            {
                return Unauthorized();
            }
            
            var chat = await _chatService.GetChatById(id, ct);

            if (chat is null)
            {
                return NotFound();
            }
            
            if (chat.UserId != userId)
            {
                return Unauthorized();
            }
            
            await _chatService.Delete(chat, ct);
            
            return Ok();
        }
    }
}
