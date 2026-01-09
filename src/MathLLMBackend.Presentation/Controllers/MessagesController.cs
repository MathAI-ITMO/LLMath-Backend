using MathLLMBackend.Core.Services.ChatService;
using MathLLMBackend.Domain.Constants;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Exceptions;
using MathLLMBackend.Presentation.Binders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MathLLMBackend.Domain.Enums;
using MathLLMBackend.Presentation.Dtos.Messages;

namespace MathLLMBackend.Presentation.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IChatService _service;
        private readonly ILogger<MessageController> _logger;

        public MessageController(IChatService service, ILogger<MessageController> logger)
        {
            _service = service;
            _logger = logger;
        }
    
        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] MessageCreateDto dto, [FromUserId] string userId, CancellationToken ct)
        {
            string llmResponseText = await _service.CreateMessageForUser(dto.ChatId, userId, dto.Text, ct);

            if (Response.HasStarted)
            {
                _logger.LogWarning("Response has already started before attempting to write LLM response for chat {ChatId}.", dto.ChatId);
                return new EmptyResult();
            }

            if (string.IsNullOrEmpty(llmResponseText))
            {
                _logger.LogWarning("LLM service returned empty or null response for chat {ChatId}, user message: {UserMessage}", dto.ChatId, dto.Text);
                return Ok(string.Empty);
            }
            
            return Ok(llmResponseText);
        }
    
        [HttpGet("get-messages-from-chat")]
        public async Task<IActionResult> GetAllMessagesFromChat(Guid chatId, [FromUserId] string userId, CancellationToken ct)
        {
            var isAdmin = User.IsInRole(RoleConstants.Admin);
            
            List<Message> messages;
            if (isAdmin)
            {
                messages = await _service.GetUserVisibleMessagesFromChatForAdmin(chatId, ct);
            }
            else
            {
                messages = await _service.GetUserVisibleMessagesFromChat(chatId, userId, ct);
            }
            
            return Ok(
                messages.Select(m => new MessageDto(m.Id, m.ChatId, m.Text, m.MessageType.ToString(), m.CreatedAt))
            );
        }
    }
}
