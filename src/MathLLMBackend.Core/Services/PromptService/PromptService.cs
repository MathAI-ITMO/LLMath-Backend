using MathLLMBackend.Core.Configuration;
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
        return ReplacePlaceholder(_promptConfiguration.TutorSolutionPrompt, "{solution}", solution);
    }

    public string GetSolverSystemPrompt()
    {
        return _promptConfiguration.SolverSystemPrompt;
    }

    public string GetSolverTaskPrompt(string task)
    {
        return ReplacePlaceholder(_promptConfiguration.SolverTaskPrompt, "{problem}", task);
    }

    public string GetDefaultSystemPrompt()
    {
        return _promptConfiguration.DefaultSystemPrompt;
    }

    private string ReplacePlaceholder(string template, string placeholder, string value)
    {
        return template.Replace(placeholder, value);
    }
}