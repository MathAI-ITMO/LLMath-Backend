using MathLLMBackend.Core.Configuration;
using MathLLMBackend.Core.Constants;
using Microsoft.Extensions.Options;

namespace MathLLMBackend.Core.Services.PromptService;

public class PromptService : IPromptService
{
    private readonly PromptConfiguration _promptConfiguration;

    public PromptService(IOptions<PromptConfiguration> promptConfiguration)
    {
        _promptConfiguration = promptConfiguration.Value;
    }

    public string GetTutorSystemPrompt()
    {
        return _promptConfiguration.TutorSystemPrompt;
    }

    public string GetTutorSolutionPrompt(string solution)
    {
        return _promptConfiguration.TutorSolutionPrompt.Replace("{solution}", solution);
    }

    public string GetSolverSystemPrompt()
    {
        return _promptConfiguration.SolverSystemPrompt;
    }

    public string GetSolverTaskPrompt(string task)
    {
        return _promptConfiguration.SolverTaskPrompt.Replace("{problem}", task);
    }

    public string GetDefaultSystemPrompt()
    {
        return _promptConfiguration.DefaultSystemPrompt;
    }
    
    public string GetLearningSystemPrompt()
    {
        return _promptConfiguration.LearningSystemPrompt;
    }
    
    public string GetGuidedSystemPrompt()
    {
        return _promptConfiguration.GuidedSystemPrompt;
    }
    
    public string GetExamSystemPrompt()
    {
        return _promptConfiguration.ExamSystemPrompt;
    }
    
    public string GetSystemPromptByTaskType(int taskType)
    {
        return taskType switch
        {
            TaskTypes.Learning => GetLearningSystemPrompt(),
            TaskTypes.Guided => GetGuidedSystemPrompt(),
            TaskTypes.Exam => GetExamSystemPrompt(),
            _ => GetTutorSystemPrompt()
        };
    }
    
    public string GetTutorInitialPrompt()
    {
        return _promptConfiguration.TutorInitialPrompt;
    }
    
    /// <summary>
    /// Gets the learning initial prompt. Note: condition and firstStep parameters are currently unused
    /// but kept for interface consistency and potential future use.
    /// </summary>
    public string GetLearningInitialPrompt(string condition, string firstStep)
    {
        // TODO: Consider using condition and firstStep parameters if prompt template supports placeholders
        return _promptConfiguration.LearningInitialPrompt;
    }
    
    public string GetGuidedInitialPrompt()
    {
        return _promptConfiguration.GuidedInitialPrompt;
    }
    
    public string GetExamInitialPrompt()
    {
        return _promptConfiguration.ExamInitialPrompt;
    }
    
    public string GetInitialPromptByTaskType(int taskType, string condition, string firstStep)
    {
        return taskType switch
        {
            TaskTypes.Learning => GetLearningInitialPrompt(condition, firstStep),
            TaskTypes.Guided => GetGuidedInitialPrompt(),
            TaskTypes.Exam => GetExamInitialPrompt(),
            _ => GetTutorInitialPrompt()
        };
    }

    public string GetExtractAnswerSystemPrompt()
    {
        return _promptConfiguration.ExtractAnswerSystemPrompt;
    }

    public string GetExtractAnswerPrompt(string problemStatement, string solution)
    {
        return _promptConfiguration.ExtractAnswerPrompt
            .Replace("{problemStatement}", problemStatement)
            .Replace("{solution}", solution);
    }
} 