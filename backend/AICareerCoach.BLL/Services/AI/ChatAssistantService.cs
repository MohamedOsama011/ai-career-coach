using System.ClientModel;
using System.Text.Json;
using AICareerCoach.BLL.DTOs.Chat;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using DbChatMessage = AICareerCoach.DAL.Entities.ChatMessage;
using DbChatMessageRole = AICareerCoach.DAL.Entities.ChatMessageRole;

namespace AICareerCoach.BLL.Services.AI
{
    public class ChatAssistantService : IChatAssistantService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly IAgentToolExecutor _toolExecutor;
        private readonly ChatClient _chatClient;
        private readonly ILogger<ChatAssistantService> _logger;
        private readonly ChatCompletionOptions _chatOptions;

        private const int MaxAgentIterations = 5;
        private const int HistoryWindow = 20;
        private const int TitleMaxChars = 50;
        private const int TimeoutSeconds = 30;

        private const string SystemPrompt = """
            You are "Coach", a friendly career assistant for the AICareerCoach app. You help users find jobs, understand their CV, and learn new skills. Match the user's language (English or Arabic). Be warm, concise, and practical.

            TOOLS:
            - get_recommended_jobs(): Get personalized job recommendations based on your CV. Uses AI-powered matching to find jobs that fit your profile. Returns top matches with match scores, explanations, and missing skills. Tell the user to upload a CV first if it returns an error.
            - get_personal_roadmap(): Get your personalized learning roadmap with gap-driven steps and seniority progression. Returns priority-ordered steps and skills gap analysis. Tell the user to generate one from the Roadmap page first if it returns an error.
            - analyze_cv(): Analyze the user's latest uploaded CV. Returns overallScore, top 5 suggestions (priority-ordered: High → Medium → Low), strengths, and missingKeywords. Returns an error if no CV is uploaded — guide the user to upload one.
            - get_user_profile(): Get the user's profile (full name, email, career goal, has_cv, roles). Call this FIRST if you're not sure whether the user has uploaded a CV.

            RULES:
            - When a user asks about their CV, skills, or job fit and you're not sure if a CV is uploaded, call get_user_profile first to check hasCV. If hasCV is false, politely tell them to upload a CV before analyze_cv will work.
            - If a tool returns { "error": "..." }, surface the message to the user in your own words. Do not expose raw JSON or stack traces.
            - Keep responses concise: 3-5 sentences for prose, bullets for lists, code blocks only when the user asks for code.
            - Do not give medical, legal, or financial advice. If asked, politely decline and suggest they consult a qualified professional.
            - Do not invent jobs, roadmaps, or CV feedback. If a tool call is needed, make the call.
            - Never claim a tool was used unless the call actually returned a result.
            """;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ChatAssistantService(
            AICareerCoachDbContext context,
            IAgentToolExecutor toolExecutor,
            IConfiguration config,
            ILogger<ChatAssistantService> logger)
        {
            _context = context;
            _toolExecutor = toolExecutor;
            _logger = logger;

            var apiKey = config["GitHub:Token"];
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("GitHub token not configured. Add 'GitHub:Token' to appsettings or user secrets.");

            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri("https://models.inference.ai.azure.com")
            };
            var credential = new ApiKeyCredential(apiKey);
            var openAiClient = new OpenAIClient(credential, options);
            _chatClient = openAiClient.GetChatClient("gpt-4o-mini");

            _chatOptions = new ChatCompletionOptions
            {
                Temperature = 0.4f,
                MaxOutputTokenCount = 800
            };
            foreach (var tool in AgentToolDefinitions.AllTools)
            {
                _chatOptions.Tools.Add(tool);
            }
        }

        public async Task<ChatSessionDto> CreateSessionAsync(string userId)
        {
            var session = new ChatSession
            {
                UserId = userId,
                Title = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();
            return BuildSessionDto(session);
        }

        public async Task<ChatSessionDto> SendMessageAsync(string userId, int sessionId, string message)
        {
            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
                ?? throw new KeyNotFoundException("Chat session not found.");

            var priorMessages = session.Messages.OrderBy(m => m.OrderIndex).ToList();

            if (session.Title == null && !string.IsNullOrWhiteSpace(message))
            {
                session.Title = message.Length > TitleMaxChars
                    ? message[..TitleMaxChars] + "…"
                    : message;
            }

            var newMessages = new List<DbChatMessage>();
            int orderIndex = priorMessages.Count;

            var userRow = new DbChatMessage
            {
                SessionId = sessionId,
                Role = DbChatMessageRole.User,
                Content = message,
                OrderIndex = orderIndex++,
                CreatedAt = DateTime.UtcNow
            };
            newMessages.Add(userRow);

            var openAiMessages = BuildOpenAiMessages(SystemPrompt, priorMessages);
            openAiMessages.Add(new UserChatMessage(message));

            var (finalText, agentMessages) = await RunAgentLoopAsync(
                userId, sessionId, openAiMessages, orderIndex);
            newMessages.AddRange(agentMessages);

            session.UpdatedAt = DateTime.UtcNow;
            _context.ChatMessages.AddRange(newMessages);
            await _context.SaveChangesAsync();

            return BuildSessionDto(session);
        }

        public async Task<ChatSessionDto> GetSessionAsync(string userId, int sessionId)
        {
            var session = await _context.ChatSessions
                .AsNoTracking()
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
                ?? throw new KeyNotFoundException("Chat session not found.");

            return BuildSessionDto(session);
        }

        public async Task<List<ChatSessionSummaryDto>> GetUserSessionsAsync(string userId)
        {
            return await _context.ChatSessions
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => new ChatSessionSummaryDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();
        }

        private List<OpenAI.Chat.ChatMessage> BuildOpenAiMessages(
            string systemPrompt, List<DbChatMessage> priorMessages)
        {
            var result = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(systemPrompt)
            };

            var recent = priorMessages.TakeLast(HistoryWindow).ToList();
            foreach (var msg in recent)
            {
                switch (msg.Role)
                {
                    case DbChatMessageRole.User:
                        result.Add(new UserChatMessage(msg.Content ?? string.Empty));
                        break;
                    case DbChatMessageRole.Assistant:
                        if (!string.IsNullOrEmpty(msg.ToolCallsJson))
                        {
                            var toolCalls = ParseToolCallsJson(msg.ToolCallsJson);
                            result.Add(new AssistantChatMessage(toolCalls));
                        }
                        else
                        {
                            result.Add(new AssistantChatMessage(msg.Content ?? string.Empty));
                        }
                        break;
                    case DbChatMessageRole.Tool:
                        result.Add(new ToolChatMessage(
                            msg.ToolCallId ?? string.Empty,
                            msg.Content ?? string.Empty));
                        break;
                }
            }
            return result;
        }

        private static string SerializeToolCalls(IReadOnlyList<ChatToolCall> toolCalls)
        {
            var dto = toolCalls.Select(tc => new PersistedToolCall
            {
                Id = tc.Id,
                FunctionName = tc.FunctionName,
                FunctionArguments = tc.FunctionArguments.ToString()
            }).ToList();
            return JsonSerializer.Serialize(dto, JsonOpts);
        }

        private static List<ChatToolCall> ParseToolCallsJson(string toolCallsJson)
        {
            using var doc = JsonDocument.Parse(toolCallsJson);
            var result = new List<ChatToolCall>();
            foreach (var tc in doc.RootElement.EnumerateArray())
            {
                var id = tc.GetProperty("id").GetString()
                    ?? throw new InvalidOperationException("Tool call id is missing.");
                var name = tc.GetProperty("functionName").GetString()
                    ?? throw new InvalidOperationException("Tool call functionName is missing.");
                var argsStr = tc.TryGetProperty("functionArguments", out var args)
                              && args.ValueKind == JsonValueKind.String
                    ? args.GetString() ?? "{}"
                    : "{}";
                result.Add(ChatToolCall.CreateFunctionToolCall(id, name, BinaryData.FromString(argsStr)));
            }
            return result;
        }

        private async Task<(string FinalText, List<DbChatMessage> NewMessages)> RunAgentLoopAsync(
            string userId, int sessionId, List<OpenAI.Chat.ChatMessage> openAiMessages, int startOrderIndex)
        {
            var newMessages = new List<DbChatMessage>();
            int orderIndex = startOrderIndex;

            for (int iteration = 1; iteration <= MaxAgentIterations; iteration++)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                ChatCompletion completion;
                try
                {
                    var response = await _chatClient.CompleteChatAsync(
                        openAiMessages, _chatOptions, cts.Token);
                    completion = response.Value;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Agent LLM call failed on iteration {Iteration} for user {UserId}.",
                        iteration, userId);
                    var fallback = "I'm having trouble responding right now. Please try again in a moment.";
                    newMessages.Add(MakeAssistantRow(sessionId, orderIndex++, fallback));
                    return (fallback, newMessages);
                }

                if (completion.FinishReason == ChatFinishReason.ToolCalls)
                {
                    newMessages.Add(new DbChatMessage
                    {
                        SessionId = sessionId,
                        Role = DbChatMessageRole.Assistant,
                        Content = null,
                        ToolCallsJson = SerializeToolCalls(completion.ToolCalls),
                        OrderIndex = orderIndex++,
                        CreatedAt = DateTime.UtcNow
                    });
                    openAiMessages.Add(new AssistantChatMessage(completion));

                    foreach (var toolCall in completion.ToolCalls)
                    {
                        var argsJson = toolCall.FunctionArguments.ToString();
                        var result = await _toolExecutor.ExecuteAsync(
                            userId, toolCall.FunctionName, argsJson);

                        newMessages.Add(new DbChatMessage
                        {
                            SessionId = sessionId,
                            Role = DbChatMessageRole.Tool,
                            Content = result,
                            ToolCallId = toolCall.Id,
                            ToolName = toolCall.FunctionName,
                            OrderIndex = orderIndex++,
                            CreatedAt = DateTime.UtcNow
                        });
                        openAiMessages.Add(new ToolChatMessage(toolCall.Id, result));
                    }
                    continue;
                }

                var finalText = completion.Content.Count > 0
                    ? completion.Content[0].Text ?? string.Empty
                    : string.Empty;

                newMessages.Add(new DbChatMessage
                {
                    SessionId = sessionId,
                    Role = DbChatMessageRole.Assistant,
                    Content = finalText,
                    OrderIndex = orderIndex++,
                    CreatedAt = DateTime.UtcNow
                });
                return (finalText, newMessages);
            }

            _logger.LogWarning(
                "Agent loop hit max iterations ({Max}) without a final answer for user {UserId}.",
                MaxAgentIterations, userId);
            var maxIterFallback = "I made several attempts but couldn't complete that. Could you rephrase or try a different question?";
            newMessages.Add(MakeAssistantRow(sessionId, orderIndex++, maxIterFallback));
            return (maxIterFallback, newMessages);
        }

        private static DbChatMessage MakeAssistantRow(int sessionId, int orderIndex, string content) =>
            new()
            {
                SessionId = sessionId,
                Role = DbChatMessageRole.Assistant,
                Content = content,
                OrderIndex = orderIndex,
                CreatedAt = DateTime.UtcNow
            };

        private static ChatSessionDto BuildSessionDto(ChatSession session)
        {
            var pendingTools = new List<string>();
            var messages = new List<ChatMessageDto>();

            var ordered = (session.Messages ?? new List<DbChatMessage>())
                .OrderBy(m => m.OrderIndex)
                .ToList();

            foreach (var msg in ordered)
            {
                switch (msg.Role)
                {
                    case DbChatMessageRole.User:
                        messages.Add(new ChatMessageDto { Role = "user", Content = msg.Content });
                        pendingTools.Clear();
                        break;
                    case DbChatMessageRole.Assistant:
                        if (string.IsNullOrEmpty(msg.Content)) continue;
                        messages.Add(new ChatMessageDto
                        {
                            Role = "assistant",
                            Content = msg.Content,
                            ToolsUsed = pendingTools.Count > 0
                                ? new List<string>(pendingTools)
                                : null
                        });
                        pendingTools.Clear();
                        break;
                    case DbChatMessageRole.Tool:
                        if (!string.IsNullOrEmpty(msg.ToolName))
                            pendingTools.Add(msg.ToolName);
                        break;
                }
            }

            return new ChatSessionDto
            {
                Id = session.Id,
                Title = session.Title,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                Messages = messages
            };
        }

        private class PersistedToolCall
        {
            public string Id { get; set; } = string.Empty;
            public string FunctionName { get; set; } = string.Empty;
            public string FunctionArguments { get; set; } = string.Empty;
        }
    }
}
