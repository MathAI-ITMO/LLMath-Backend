using MathLLMBackend.Core.Services.AuthService;
using MathLLMBackend.Presentation.Dtos.Auth;
using MathLLMBackend.Presentation.Dtos.Common;
using Microsoft.AspNetCore.Mvc;

namespace MathLLMBackend.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _authService.RegisterAsync(
                    registerDto.Email,
                    registerDto.Password,
                    registerDto.FirstName,
                    registerDto.LastName,
                    registerDto.StudentGroup,
                    ct);

                return Ok(new UserInfoDto(
                    Guid.Parse(user.Id),
                    user.Email ?? string.Empty,
                    user.FirstName,
                    user.LastName,
                    user.StudentGroup));
            }
            catch (InvalidOperationException ex)
            {
                var errorMessage = ex.Message;
                var isDuplicate = errorMessage.Contains("уже существует");
                
                return BadRequest(new 
                {
                    errors = new Dictionary<string, string[]>
                    {
                        { "", new[] { errorMessage } }
                    },
                    title = "Ошибка регистрации",
                    status = 400,
                    detail = isDuplicate 
                        ? errorMessage 
                        : "Не удалось создать аккаунт. Пожалуйста, исправьте ошибки и попробуйте снова."
                });
            }
        }
    }
} 