using MathLLMBackend.Core.Constants;
using MathLLMBackend.Core.Services;
using MathLLMBackend.Presentation.Dtos.Tasks;
using MathLLMBackend.Presentation.Binders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathLLMBackend.Presentation.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UserTasksController : ControllerBase
{
    private readonly IUserTaskService _userTaskService;
    private readonly ILogger<UserTasksController> _logger;

    public UserTasksController(
        IUserTaskService userTaskService, 
        ILogger<UserTasksController> logger)
    {
        _userTaskService = userTaskService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserTasks([FromUserId] string userId, [FromQuery] int taskType = TaskTypes.Default)
    {
        var tasks = await _userTaskService.GetOrCreateUserTasksAsync(userId, taskType, HttpContext.RequestAborted);
        var dtos = tasks.Select(t => new UserTaskDto(
            t.Id,
            t.ProblemId,
            t.DisplayName,
            t.TaskType,
            t.Status,
            t.AssociatedChatId
        ));
        return Ok(dtos);
    }

    [HttpPost("{userTaskId:guid}/start")]
    [ProducesResponseType(typeof(UserTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartUserTask(Guid userTaskId, [FromUserId] string userId, CancellationToken ct = default)
    {
        var task = await _userTaskService.StartUserTaskWithChatAsync(userTaskId, userId, ct);
        
        var dto = new UserTaskDto(
            task.Id,
            task.ProblemId,
            task.DisplayName,
            task.TaskType,
            task.Status,
            task.AssociatedChatId
        );
        
        return Ok(dto);
    }

    [HttpPost("{userTaskId:guid}/complete")]
    [ProducesResponseType(typeof(UserTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteUserTask(Guid userTaskId, [FromUserId] string userId)
    {
        var completedTask = await _userTaskService.CompleteTaskAsync(userTaskId, userId, HttpContext.RequestAborted);
        if (completedTask == null)
        {
            return NotFound();
        }
        
        var dto = new UserTaskDto(
            completedTask.Id,
            completedTask.ProblemId,
            completedTask.DisplayName,
            completedTask.TaskType,
            completedTask.Status,
            completedTask.AssociatedChatId
        );
        
        return Ok(dto);
    }
} 