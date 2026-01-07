using MathLLMBackend.Core.Models;

namespace MathLLMBackend.Core.Services.StatsService;

public interface IStatsService
{
    Task<Dictionary<string, string>> GetTaskModeTitlesAsync(CancellationToken ct = default);
    Task<IEnumerable<UserStats>> GetUserStatsAsync(CancellationToken ct = default);
    Task<UserDetail> GetUserDetailsAsync(string userId, CancellationToken ct = default);
}
