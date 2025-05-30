using System.Text;
using System.Text.Json;
using MathLLMBackend.Core.Configuration;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MathLLMBackend.Core.Services.LlmService;

/// <summary>
/// Реализация сервиса логирования взаимодействий с LLM
/// </summary>
public class LlmLoggingService : ILlmLoggingService
{
    private readonly ILogger<LlmLoggingService> _logger;
    private readonly LlmLoggingConfiguration _config;
    
    public LlmLoggingService(
        ILogger<LlmLoggingService> logger,
        IOptions<LlmLoggingConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }
    
    /// <inheritdoc />
    public async Task LogInteraction(int taskType, IEnumerable<Message> messages, string response, string modelName)
    {
        if (!_config.Enabled)
        {
            return;
        }
        
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"==== LLM INTERACTION LOG - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ====");
            sb.AppendLine($"Task Type: {GetTaskTypeName(taskType)} (Code: {taskType})");
            sb.AppendLine($"Model: {modelName}");
            sb.AppendLine("\n--- MESSAGES SENT TO LLM ---");
            
            foreach (var message in messages)
            {
                sb.AppendLine($"[{message.MessageType}] {(message.IsSystemPrompt ? "(SYSTEM PROMPT)" : "")}");
                sb.AppendLine(message.Text);
                sb.AppendLine("-----------------------------");
            }
            
            sb.AppendLine("\n--- LLM RESPONSE ---");
            sb.AppendLine(response);
            sb.AppendLine("=============================================\n\n");
            
            // Проверяем наличие директории для логов и создаем ее при необходимости
            var directory = Path.GetDirectoryName(_config.LogFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Записываем в файл с добавлением
            await File.AppendAllTextAsync(_config.LogFilePath, sb.ToString());
            
            _logger.LogDebug("LLM interaction logged to {LogFilePath}", _config.LogFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log LLM interaction");
        }
    }
    
    /// <inheritdoc />
    public async Task LogSolution(string problem, string solution, string modelName)
    {
        if (!_config.Enabled)
        {
            return;
        }
        
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"==== LLM SOLUTION LOG - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ====");
            sb.AppendLine($"Model: {modelName}");
            sb.AppendLine("\n--- PROBLEM ---");
            sb.AppendLine(problem);
            sb.AppendLine("\n--- SOLUTION GENERATED BY LLM ---");
            sb.AppendLine(solution);
            sb.AppendLine("=============================================\n\n");
            
            // Проверяем наличие директории для логов и создаем ее при необходимости
            var directory = Path.GetDirectoryName(_config.LogFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Записываем в файл с добавлением
            await File.AppendAllTextAsync(_config.LogFilePath, sb.ToString());
            
            _logger.LogDebug("LLM solution logged to {LogFilePath}", _config.LogFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log LLM solution");
        }
    }
    
    /// <summary>
    /// Получает наименование типа задачи по его коду
    /// </summary>
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
} 