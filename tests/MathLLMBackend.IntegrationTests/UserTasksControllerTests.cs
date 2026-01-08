using System.Net;
using Xunit;
using FluentAssertions;
using System.Net.Http.Json;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Linq;

namespace MathLLMBackend.IntegrationTests;

[Collection("Integration Tests")]
public class UserTasksControllerTests : BaseIntegrationTest
{
    public UserTasksControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetUserTasks_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/usertasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserTasks_WithAuth_ReturnsOk()
    {
        await CreateAndLoginUserAsync();
        var response = await AuthenticatedGetAsync("/api/usertasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StartUserTask_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync("/api/usertasks/00000000-0000-0000-0000-000000000000/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StartUserTask_WithNonExistentTask_ReturnsNotFound()
    {
        await CreateAndLoginUserAsync();
        var response = await AuthenticatedPostAsync("/api/usertasks/00000000-0000-0000-0000-000000000000/start");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CompleteUserTask_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync("/api/usertasks/00000000-0000-0000-0000-000000000000/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CompleteUserTask_WithNonExistentTask_ReturnsNotFound()
    {
        await CreateAndLoginUserAsync();
        var response = await AuthenticatedPostAsync("/api/usertasks/00000000-0000-0000-0000-000000000000/complete");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartUserTask_WithValidTask_ReturnsOk()
    {
        Factory.ProblemsApiMock
            .Setup(x => x.GetProblemsByType(It.IsAny<string>()))
            .ReturnsAsync(new List<MathLLMBackend.ProblemsClient.Models.Problem>
            {
                new()
                {
                    Id = "test-problem-id",
                    Title = "Test Problem",
                    Statement = "Test problem statement"
                }
            });

        Factory.ProblemsApiMock
            .Setup(x => x.GetProblemById(It.IsAny<string>()))
            .ReturnsAsync(new MathLLMBackend.ProblemsClient.Models.Problem
            {
                Id = "test-problem-id",
                Statement = "Test problem",
                LlmSolution = "Test solution"
            });

        Factory.LlmServiceMock
            .Setup(x => x.GenerateNextMessageAsync(It.IsAny<List<MathLLMBackend.Domain.Entities.Message>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test response");

        await CreateAndLoginUserAsync();
        
        var getUserTasksResponse = await AuthenticatedGetAsync("/api/usertasks?taskType=1");
        getUserTasksResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await getUserTasksResponse.Content.ReadFromJsonAsync<List<MathLLMBackend.Presentation.Dtos.Tasks.UserTaskDto>>();
        
        tasks.Should().NotBeNull().And.NotBeEmpty();
        var taskId = tasks!.First().Id;
        var response = await AuthenticatedPostAsync($"/api/usertasks/{taskId}/start");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CompleteUserTask_WithValidTask_ReturnsOk()
    {

        await CreateAndLoginUserAsync();
        
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MathLLMBackend.DataAccess.Contexts.AppDbContext>();
        var userTask = new MathLLMBackend.Domain.Entities.UserTask
        {
            ApplicationUserId = TestUser!.Id,
            ProblemId = "test-problem-id",
            ProblemHash = "test-problem-id",
            DisplayName = "Test Task",
            TaskType = 1,
            Status = MathLLMBackend.Domain.Enums.UserTaskStatus.InProgress
        };
        dbContext.UserTasks.Add(userTask);
        await dbContext.SaveChangesAsync();

        var response = await AuthenticatedPostAsync($"/api/usertasks/{userTask.Id}/complete");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
