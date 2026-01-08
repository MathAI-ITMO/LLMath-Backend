using FluentAssertions;
using MathLLMBackend.Core.Services;
using MathLLMBackend.Core.Services.ChatService;
using MathLLMBackend.Core.Services.ProblemsService;
using MathLLMBackend.DataAccess.Contexts;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using MathLLMBackend.ProblemsClient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MathLLMBackend.Tests.Services;

public class UserTaskServiceTests
{
    private readonly AppDbContext _context;
    private readonly Mock<IProblemsService> _problemsServiceMock;
    private readonly Mock<IChatService> _chatServiceMock;
    private readonly Mock<ILogger<UserTaskService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly UserTaskService _service;

    public UserTaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _problemsServiceMock = new Mock<IProblemsService>();
        _chatServiceMock = new Mock<IChatService>();
        _loggerMock = new Mock<ILogger<UserTaskService>>();

        var configDict = new Dictionary<string, string?>
        {
            { "TaskModeTitles:1", "Learning" },
            { "TaskModeTitles:2", "Guided" },
            { "TaskModeTitles:3", "Exam" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        _service = new UserTaskService(
            _context,
            _problemsServiceMock.Object,
            _chatServiceMock.Object,
            _loggerMock.Object,
            _configuration);
    }

    [Fact]
    public async Task GetOrCreateUserTasksAsync_WhenTaskTypeNotInConfig_ReturnsEmptyList()
    {
        const string userId = "user1";
        const int taskType = 99;

        var result = await _service.GetOrCreateUserTasksAsync(userId, taskType);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrCreateUserTasksAsync_WhenProblemsServiceThrows_ReturnsEmptyList()
    {
        const string userId = "user1";
        const int taskType = 1;

        _problemsServiceMock
            .Setup(x => x.GetSavedProblemsByTypes(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        var result = await _service.GetOrCreateUserTasksAsync(userId, taskType);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrCreateUserTasksAsync_WhenNoProblemsFound_ReturnsEmptyList()
    {
        const string userId = "user1";
        const int taskType = 1;

        _problemsServiceMock
            .Setup(x => x.GetSavedProblemsByTypes(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Problem>());

        var result = await _service.GetOrCreateUserTasksAsync(userId, taskType);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrCreateUserTasksAsync_WhenProblemHasEmptyId_SkipsProblem()
    {
        const string userId = "user1";
        const int taskType = 1;

        var problems = new List<Problem>
        {
            new() { Id = "", Statement = "Test", Title = "Test Title" },
            new() { Id = "problem2", Statement = "Test 2", Title = "Test Title 2" }
        };

        _problemsServiceMock
            .Setup(x => x.GetSavedProblemsByTypes(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(problems);

        var result = await _service.GetOrCreateUserTasksAsync(userId, taskType);

        result.Should().HaveCount(1);
        result.First().ProblemId.Should().Be("problem2");
    }

    [Fact]
    public async Task GetOrCreateUserTasksAsync_CreatesNewUserTasks_WhenNoneExist()
    {
        const string userId = "user1";
        const int taskType = 1;

        var problems = new List<Problem>
        {
            new() { Id = "problem1", Statement = "Test Statement", Title = "Test Title" }
        };

        _problemsServiceMock
            .Setup(x => x.GetSavedProblemsByTypes(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(problems);

        var result = await _service.GetOrCreateUserTasksAsync(userId, taskType);

        result.Should().HaveCount(1);
        var task = result.First();
        task.ProblemId.Should().Be("problem1");
        task.DisplayName.Should().Be("Test Title");
        task.TaskType.Should().Be(taskType);
        task.Status.Should().Be(UserTaskStatus.NotStarted);
        task.AssociatedChatId.Should().BeNull();

        var dbTask = await _context.UserTasks.FirstOrDefaultAsync(ut => ut.Id == task.Id);
        dbTask.Should().NotBeNull();
        dbTask!.ApplicationUserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetOrCreateUserTasksAsync_WhenTaskExists_ReturnsExistingTask()
    {
        const string userId = "user1";
        const int taskType = 1;
        const string problemId = "problem1";

        var existingTask = new UserTask
        {
            ApplicationUserId = userId,
            ProblemId = problemId,
            ProblemHash = problemId,
            DisplayName = "Existing Task",
            TaskType = taskType,
            Status = UserTaskStatus.InProgress
        };

        _context.UserTasks.Add(existingTask);
        await _context.SaveChangesAsync();

        var problems = new List<Problem>
        {
            new() { Id = problemId, Statement = "Test Statement", Title = "Test Title" }
        };

        _problemsServiceMock
            .Setup(x => x.GetSavedProblemsByTypes(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(problems);

        var result = await _service.GetOrCreateUserTasksAsync(userId, taskType);

        result.Should().HaveCount(1);
        result.First().Id.Should().Be(existingTask.Id);
        result.First().DisplayName.Should().Be("Existing Task");
        result.First().Status.Should().Be(UserTaskStatus.InProgress);

        var dbTasksCount = await _context.UserTasks.CountAsync();
        dbTasksCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateUserTasksAsync_WhenProblemHasNoTitle_UsesStatementSnippet()
    {
        const string userId = "user1";
        const int taskType = 1;
        var longStatement = new string('a', 100);

        var problems = new List<Problem>
        {
            new() { Id = "problem1", Statement = longStatement, Title = "" }
        };

        _problemsServiceMock
            .Setup(x => x.GetSavedProblemsByTypes(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(problems);

        var result = await _service.GetOrCreateUserTasksAsync(userId, taskType);

        result.Should().HaveCount(1);
        result.First().DisplayName.Should().HaveLength(53);
        result.First().DisplayName.Should().EndWith("...");
    }

    [Fact]
    public async Task StartTaskAsync_WhenTaskNotFound_ReturnsNull()
    {
        var taskId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        const string userId = "user1";

        var result = await _service.StartTaskAsync(taskId, chatId, userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task StartTaskAsync_WhenTaskBelongsToDifferentUser_ReturnsNull()
    {
        const string userId = "user1";
        const string otherUserId = "user2";
        var chatId = Guid.NewGuid();

        var task = new UserTask
        {
            ApplicationUserId = userId,
            ProblemId = "problem1",
            ProblemHash = "problem1",
            DisplayName = "Test Task",
            TaskType = 1,
            Status = UserTaskStatus.NotStarted
        };

        _context.UserTasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _service.StartTaskAsync(task.Id, chatId, otherUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task StartTaskAsync_WhenTaskAlreadyInProgressWithSameChat_ReturnsTask()
    {
        const string userId = "user1";
        var chatId = Guid.NewGuid();

        var task = new UserTask
        {
            ApplicationUserId = userId,
            ProblemId = "problem1",
            ProblemHash = "problem1",
            DisplayName = "Test Task",
            TaskType = 1,
            Status = UserTaskStatus.InProgress,
            AssociatedChatId = chatId
        };

        _context.UserTasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _service.StartTaskAsync(task.Id, chatId, userId);

        result.Should().NotBeNull();
        result!.Status.Should().Be(UserTaskStatus.InProgress);
        result.AssociatedChatId.Should().Be(chatId);
    }

    [Fact]
    public async Task StartTaskAsync_WhenTaskAssociatedWithDifferentChat_ReturnsNull()
    {
        const string userId = "user1";
        var existingChatId = Guid.NewGuid();
        var newChatId = Guid.NewGuid();

        var task = new UserTask
        {
            ApplicationUserId = userId,
            ProblemId = "problem1",
            ProblemHash = "problem1",
            DisplayName = "Test Task",
            TaskType = 1,
            Status = UserTaskStatus.InProgress,
            AssociatedChatId = existingChatId
        };

        _context.UserTasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _service.StartTaskAsync(task.Id, newChatId, userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task StartTaskAsync_UpdatesTaskStatusToInProgress()
    {
        const string userId = "user1";
        var chatId = Guid.NewGuid();

        var task = new UserTask
        {
            ApplicationUserId = userId,
            ProblemId = "problem1",
            ProblemHash = "problem1",
            DisplayName = "Test Task",
            TaskType = 1,
            Status = UserTaskStatus.NotStarted
        };

        _context.UserTasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _service.StartTaskAsync(task.Id, chatId, userId);

        result.Should().NotBeNull();
        result!.Status.Should().Be(UserTaskStatus.InProgress);
        result.AssociatedChatId.Should().Be(chatId);

        var dbTask = await _context.UserTasks.FindAsync(task.Id);
        dbTask!.Status.Should().Be(UserTaskStatus.InProgress);
        dbTask.AssociatedChatId.Should().Be(chatId);
    }

    [Fact]
    public async Task GetUserTaskByIdAsync_WhenTaskNotFound_ReturnsNull()
    {
        var taskId = Guid.NewGuid();
        const string userId = "user1";

        var result = await _service.GetUserTaskByIdAsync(taskId, userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserTaskByIdAsync_WhenTaskBelongsToDifferentUser_ReturnsNull()
    {
        const string userId = "user1";
        const string otherUserId = "user2";

        var task = new UserTask
        {
            ApplicationUserId = userId,
            ProblemId = "problem1",
            ProblemHash = "problem1",
            DisplayName = "Test Task",
            TaskType = 1,
            Status = UserTaskStatus.NotStarted
        };

        _context.UserTasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _service.GetUserTaskByIdAsync(task.Id, otherUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserTaskByIdAsync_WhenTaskExists_ReturnsTask()
    {
        const string userId = "user1";

        var task = new UserTask
        {
            ApplicationUserId = userId,
            ProblemId = "problem1",
            ProblemHash = "problem1",
            DisplayName = "Test Task",
            TaskType = 1,
            Status = UserTaskStatus.InProgress
        };

        _context.UserTasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _service.GetUserTaskByIdAsync(task.Id, userId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.DisplayName.Should().Be("Test Task");
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTaskNotFound_ReturnsNull()
    {
        var taskId = Guid.NewGuid();
        const string userId = "user1";

        var result = await _service.CompleteTaskAsync(taskId, userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTaskBelongsToDifferentUser_ReturnsNull()
    {
        const string userId = "user1";
        const string otherUserId = "user2";

        var task = new UserTask
        {
            ApplicationUserId = userId,
            ProblemId = "problem1",
            ProblemHash = "problem1",
            DisplayName = "Test Task",
            TaskType = 1,
            Status = UserTaskStatus.InProgress
        };

        _context.UserTasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _service.CompleteTaskAsync(task.Id, otherUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTaskAlreadySolved_ReturnsTask()
    {
        const string userId = "user1";

        var task = new UserTask
        {
            ApplicationUserId = userId,
            ProblemId = "problem1",
            ProblemHash = "problem1",
            DisplayName = "Test Task",
            TaskType = 1,
            Status = UserTaskStatus.Solved
        };

        _context.UserTasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _service.CompleteTaskAsync(task.Id, userId);

        result.Should().NotBeNull();
        result!.Status.Should().Be(UserTaskStatus.Solved);
    }

    [Fact]
    public async Task CompleteTaskAsync_UpdatesTaskStatusToSolved()
    {
        const string userId = "user1";

        var task = new UserTask
        {
            ApplicationUserId = userId,
            ProblemId = "problem1",
            ProblemHash = "problem1",
            DisplayName = "Test Task",
            TaskType = 1,
            Status = UserTaskStatus.InProgress
        };

        _context.UserTasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _service.CompleteTaskAsync(task.Id, userId);

        result.Should().NotBeNull();
        result!.Status.Should().Be(UserTaskStatus.Solved);

        var dbTask = await _context.UserTasks.FindAsync(task.Id);
        dbTask!.Status.Should().Be(UserTaskStatus.Solved);
    }
}
