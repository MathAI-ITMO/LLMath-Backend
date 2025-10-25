using System.ClientModel;
using MathLLMBackend.Core.Configuration;
using MathLLMBackend.Core.Services.PromptService;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.Runtime.CompilerServices;
using System.Text;

namespace MathLLMBackend.Core.Services.LlmService;

public class LlmService : ILlmService
{
    private readonly IOptions<LlmServiceConfiguration> _config;
    private readonly IPromptService _promptService;
    private readonly ILogger<LlmService> _logger;

    public LlmService(
        IOptions<LlmServiceConfiguration> config,
        IPromptService promptService,
        ILogger<LlmService> logger)
    {
        _config = config;
        _promptService = promptService;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> GenerateNextMessageStreaming(List<Message> messages, int taskType, [EnumeratorCancellation] CancellationToken ct)
    {
        var config = _config.Value.ChatModel;

        var client = new ChatClient(
            model: config.Model,
            credential: new ApiKeyCredential(config.Token),
            options: new OpenAIClientOptions() { Endpoint = new Uri(config.Url) });

        var openaiMessages = messages.Select<Message, ChatMessage>(m =>
            m.MessageType switch
            {
                MessageType.User => new UserChatMessage(m.Text),
                MessageType.Assistant => new AssistantChatMessage(m.Text),
                MessageType.System => new SystemChatMessage(m.Text),
                _ => throw new NotImplementedException()
            }
        );

        var fullResponseText = new StringBuilder();
        AsyncCollectionResult<StreamingChatCompletionUpdate> completion = client.CompleteChatStreamingAsync(openaiMessages, cancellationToken: ct);

        await foreach (var chunk in completion)
        {
            ct.ThrowIfCancellationRequested();
            if (chunk.ContentUpdate.Count > 0 && !string.IsNullOrEmpty(chunk.ContentUpdate[0].Text))
            {
                var textChunk = chunk.ContentUpdate[0].Text;
                fullResponseText.Append(textChunk);
                _logger.LogInformation("LlmService: Streaming chunk: {textChunk}", textChunk);
                yield return textChunk;
            }
        }
        // Log the full interaction after streaming is complete
        LogInteraction(taskType, messages, fullResponseText.ToString(), config.Model);
    }

    public async Task<string> SolveProblem(string problemDescription, CancellationToken ct)
    {
        var config = _config.Value.SolverModel;

        var client = new ChatClient(
            model: config.Model,
            credential: new ApiKeyCredential(config.Token),
            options: new OpenAIClientOptions() { Endpoint = new Uri(config.Url) });

        var solverSystemPrompt = _promptService.GetSolverSystemPrompt();
        var solverTaskPrompt = _promptService.GetSolverTaskPrompt(problemDescription);

        var openaiMessages = new ChatMessage[]
            {
                new SystemChatMessage(solverSystemPrompt),
                new UserChatMessage(solverTaskPrompt),
            };

        var completion = await client.CompleteChatAsync(openaiMessages, cancellationToken: ct);
        var solution = completion!.Value.Content[0].Text;

        LogSolution(problemDescription, solution, config.Model);

        return solution;
    }

    public async Task<string> GenerateNextMessageAsync(List<Message> messages, int taskType, CancellationToken ct)
    {
        var config = _config.Value.ChatModel;

        var client = new ChatClient(
            model: config.Model,
            credential: new ApiKeyCredential(config.Token),
            options: new OpenAIClientOptions() { Endpoint = new Uri(config.Url) });

        var openaiMessages = messages.Select<Message, ChatMessage>(m =>
            m.MessageType switch
            {
                MessageType.User => new UserChatMessage(m.Text),
                MessageType.Assistant => new AssistantChatMessage(m.Text),
                MessageType.System => new SystemChatMessage(m.Text),
                _ => throw new NotImplementedException()
            }
        );

        var completion = await client.CompleteChatAsync(openaiMessages, cancellationToken: ct);
        var response = completion!.Value.Content[0].Text;

        LogInteraction(taskType, messages, response, config.Model);

        return response;
    }

    public async Task<string> ExtractAnswer(string problemStatement, string solution, CancellationToken ct)
    {
        var config = _config.Value.SolverModel;

        var client = new ChatClient(
            model: config.Model,
            credential: new ApiKeyCredential(config.Token),
            options: new OpenAIClientOptions() { Endpoint = new Uri(config.Url) });

        var extractAnswerSystemPrompt = _promptService.GetExtractAnswerSystemPrompt();
        var extractAnswerPrompt = _promptService.GetExtractAnswerPrompt(problemStatement, solution);

        var openaiMessages = new ChatMessage[]
            {
                new SystemChatMessage(extractAnswerSystemPrompt),
                new UserChatMessage(extractAnswerPrompt),
            };

        var completion = await client.CompleteChatAsync(openaiMessages, cancellationToken: ct);
        var extractedAnswer = completion!.Value.Content[0].Text;

        _logger.LogInformation("Extracted answer: {ExtractedAnswer}", extractedAnswer);

        return extractedAnswer;
    }

    private void LogInteraction(int taskType, IEnumerable<Message> messages, string response, string modelName)
    {
        var taskTypeName = GetTaskTypeName(taskType);
        
        _logger.LogDebug(
            "LLM Interaction - Task: {TaskTypeName} ({TaskType}), Model: {Model}, Response: {Response}",
            taskTypeName, taskType, modelName, response);

        _logger.LogTrace(
            "LLM Interaction Details - Task: {TaskTypeName} ({TaskType}), Model: {Model}\nMessages:\n{Messages}\nResponse:\n{Response}",
            taskTypeName, taskType, modelName, 
            FormatMessages(messages), response);
    }

    private void LogSolution(string problem, string solution, string modelName)
    {
        _logger.LogDebug(
            "LLM Solution - Model: {Model}, Problem: {Problem}, Solution: {Solution}",
            modelName, problem, solution);

        _logger.LogTrace(
            "LLM Solution Details - Model: {Model}\nProblem:\n{Problem}\nSolution:\n{Solution}",
            modelName, problem, solution);
    }

    private static string GetTaskTypeName(int taskType)
    {
        return taskType switch
        {
            0 => "Default (Tutor)",
            1 => "Learning",
            2 => "Guided",
            3 => "Exam",
            _ => $"Unknown ({taskType})"
        };
    }

    private static string FormatMessages(IEnumerable<Message> messages)
    {
        var sb = new StringBuilder();
        foreach (var message in messages)
        {
            sb.AppendLine($"[{message.MessageType}] {(message.IsSystemPrompt ? "(SYSTEM)" : "")} {message.Text}");
        }
        return sb.ToString();
    }
}