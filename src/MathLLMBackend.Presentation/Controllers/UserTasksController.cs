using MathLLMBackend.Core.Constants;
using MathLLMBackend.Core.Services;
using MathLLMBackend.Core.Dtos;
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

    /// <summary>
    /// Получает или создает задачи пользователя для указанного типа.
    /// </summary>
    /// <param name="taskType">Тип задач (например, 0 для задач из списка "Выбрать задачу").</param>
    /// <returns>Список задач пользователя.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserTasks([FromUserId] string userId, [FromQuery] int taskType = TaskTypes.Default)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting or creating user tasks for user {UserId} and type {TaskType}", userId, taskType);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Помечает задачу как начатую (InProgress) и создает/связывает ее с чатом.
    /// </summary>
    /// <param name="userTaskId">ID задачи пользователя.</param>
    /// <returns>Обновленная задача пользователя.</returns>
    [HttpPost("{userTaskId:guid}/start")]
    [ProducesResponseType(typeof(UserTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartUserTask(Guid userTaskId, [FromUserId] string userId, CancellationToken ct = default)
    {
        try
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
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found") || ex.Message.Contains("don't have permission"))
            {
                return NotFound(ex.Message);
            }
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting user task {UserTaskId} for user {UserId}", userTaskId, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Помечает задачу как решенную (Solved).
    /// </summary>
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