using FluentAssertions;
using MathLLMBackend.Core.Configuration;
using MathLLMBackend.Core.Services.PromptService;
using Microsoft.Extensions.Options;
using Xunit;

namespace MathLLMBackend.Tests.Services;

public class PromptServiceTests
{
    private readonly PromptConfiguration _config;
    private readonly PromptService _service;

    public PromptServiceTests()
    {
        _config = new PromptConfiguration
        {
            TutorSystemPrompt = "Tutor System Prompt",
            TutorSolutionPrompt = "Solution: {solution}",
            SolverSystemPrompt = "Solver System Prompt",
            SolverTaskPrompt = "Task: {problem}",
            DefaultSystemPrompt = "Default System Prompt",
            LearningSystemPrompt = "Learning System Prompt",
            GuidedSystemPrompt = "Guided System Prompt",
            ExamSystemPrompt = "Exam System Prompt",
            TutorInitialPrompt = "Tutor Initial Prompt",
            LearningInitialPrompt = "Learning Initial Prompt",
            GuidedInitialPrompt = "Guided Initial Prompt",
            ExamInitialPrompt = "Exam Initial Prompt",
            ExtractAnswerSystemPrompt = "Extract Answer System Prompt",
            ExtractAnswerPrompt = "Problem: {problemStatement}, Solution: {solution}"
        };

        var options = Options.Create(_config);
        _service = new PromptService(options);
    }

    [Fact]
    public void GetTutorSystemPrompt_ReturnsConfiguredPrompt()
    {
        var result = _service.GetTutorSystemPrompt();

        result.Should().Be(_config.TutorSystemPrompt);
    }

    [Fact]
    public void GetTutorSolutionPrompt_ReplacesSolutionPlaceholder()
    {
        const string solution = "Test Solution";
        var result = _service.GetTutorSolutionPrompt(solution);

        result.Should().Be("Solution: Test Solution");
        result.Should().Contain(solution);
    }

    [Fact]
    public void GetSolverTaskPrompt_ReplacesProblemPlaceholder()
    {
        const string problem = "Test Problem";
        var result = _service.GetSolverTaskPrompt(problem);

        result.Should().Be("Task: Test Problem");
        result.Should().Contain(problem);
    }

    [Fact]
    public void GetSystemPromptByTaskType_ReturnsCorrectPromptForTaskType0()
    {
        var result = _service.GetSystemPromptByTaskType(0);

        result.Should().Be(_config.TutorSystemPrompt);
    }

    [Fact]
    public void GetSystemPromptByTaskType_ReturnsCorrectPromptForTaskType1()
    {
        var result = _service.GetSystemPromptByTaskType(1);

        result.Should().Be(_config.LearningSystemPrompt);
    }

    [Fact]
    public void GetSystemPromptByTaskType_ReturnsCorrectPromptForTaskType2()
    {
        var result = _service.GetSystemPromptByTaskType(2);

        result.Should().Be(_config.GuidedSystemPrompt);
    }

    [Fact]
    public void GetSystemPromptByTaskType_ReturnsCorrectPromptForTaskType3()
    {
        var result = _service.GetSystemPromptByTaskType(3);

        result.Should().Be(_config.ExamSystemPrompt);
    }

    [Fact]
    public void GetSystemPromptByTaskType_ReturnsTutorPromptForUnknownTaskType()
    {
        var result = _service.GetSystemPromptByTaskType(99);

        result.Should().Be(_config.TutorSystemPrompt);
    }

    [Fact]
    public void GetInitialPromptByTaskType_ReturnsCorrectPromptForTaskType0()
    {
        var result = _service.GetInitialPromptByTaskType(0, "condition", "firstStep");

        result.Should().Be(_config.TutorInitialPrompt);
    }

    [Fact]
    public void GetInitialPromptByTaskType_ReturnsCorrectPromptForTaskType1()
    {
        var result = _service.GetInitialPromptByTaskType(1, "condition", "firstStep");

        result.Should().Be(_config.LearningInitialPrompt);
    }

    [Fact]
    public void GetInitialPromptByTaskType_ReturnsCorrectPromptForTaskType2()
    {
        var result = _service.GetInitialPromptByTaskType(2, "condition", "firstStep");

        result.Should().Be(_config.GuidedInitialPrompt);
    }

    [Fact]
    public void GetInitialPromptByTaskType_ReturnsCorrectPromptForTaskType3()
    {
        var result = _service.GetInitialPromptByTaskType(3, "condition", "firstStep");

        result.Should().Be(_config.ExamInitialPrompt);
    }

    [Fact]
    public void GetExtractAnswerPrompt_ReplacesBothPlaceholders()
    {
        const string problemStatement = "Test Problem";
        const string solution = "Test Solution";
        var result = _service.GetExtractAnswerPrompt(problemStatement, solution);

        result.Should().Contain(problemStatement);
        result.Should().Contain(solution);
        result.Should().Be("Problem: Test Problem, Solution: Test Solution");
    }

    [Fact]
    public void GetAllPromptMethods_ReturnNonEmptyStrings()
    {
        _service.GetTutorSystemPrompt().Should().NotBeNullOrEmpty();
        _service.GetSolverSystemPrompt().Should().NotBeNullOrEmpty();
        _service.GetDefaultSystemPrompt().Should().NotBeNullOrEmpty();
        _service.GetLearningSystemPrompt().Should().NotBeNullOrEmpty();
        _service.GetGuidedSystemPrompt().Should().NotBeNullOrEmpty();
        _service.GetExamSystemPrompt().Should().NotBeNullOrEmpty();
        _service.GetTutorInitialPrompt().Should().NotBeNullOrEmpty();
        _service.GetGuidedInitialPrompt().Should().NotBeNullOrEmpty();
        _service.GetExamInitialPrompt().Should().NotBeNullOrEmpty();
        _service.GetExtractAnswerSystemPrompt().Should().NotBeNullOrEmpty();
    }
}
