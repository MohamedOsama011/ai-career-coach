namespace AICareerCoach.BLL.Interfaces.AI
{
    public interface IAgentToolExecutor
    {
        Task<string> ExecuteAsync(string userId, string toolName, string argumentsJson);
    }
}
