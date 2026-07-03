using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using Microsoft.Extensions.Logging;

namespace AICareerCoach.BLL.Services.AI
{
    public class AgentToolExecutor : IAgentToolExecutor
    {
        private readonly IJobService _jobService;
        private readonly IRoadmapService _roadmapService;
        private readonly ICvFeedbackService _cvFeedbackService;
        private readonly IUserService _userService;
        private readonly ILogger<AgentToolExecutor> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AgentToolExecutor(
            IJobService jobService,
            IRoadmapService roadmapService,
            ICvFeedbackService cvFeedbackService,
            IUserService userService,
            ILogger<AgentToolExecutor> logger)
        {
            _jobService = jobService;
            _roadmapService = roadmapService;
            _cvFeedbackService = cvFeedbackService;
            _userService = userService;
            _logger = logger;
        }

        public async Task<string> ExecuteAsync(string userId, string toolName, string argumentsJson)
        {
            try
            {
                return toolName switch
                {
                    "search_jobs" => await ExecuteSearchJobsAsync(argumentsJson),
                    "get_career_roadmap" => await ExecuteGetCareerRoadmapAsync(argumentsJson),
                    "analyze_cv" => await ExecuteAnalyzeCvAsync(userId),
                    "get_user_profile" => await ExecuteGetUserProfileAsync(userId),
                    _ => throw new InvalidOperationException($"Unknown tool '{toolName}'.")
                };
            }
            catch (Exception ex) when (ex is KeyNotFoundException
                                    || ex is InvalidOperationException
                                    || ex is ArgumentException)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Tool {Tool} failed for user {UserId}: {Exception}",
                    toolName, userId, ex);
                return JsonSerializer.Serialize(
                    new { error = $"Tool '{toolName}' failed temporarily." }, JsonOpts);
            }
        }

        private async Task<string> ExecuteSearchJobsAsync(string argumentsJson)
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;
            var query = root.GetProperty("query").GetString()
                ?? throw new ArgumentException("query is required.");
            string? location = root.TryGetProperty("location", out var loc) && loc.ValueKind == JsonValueKind.String
                ? loc.GetString()
                : null;

            var filter = new JobFilterDto
            {
                Search = query,
                Location = location,
                Page = 1,
                PageSize = 3
            };

            var page = await _jobService.GetJobsAsync(filter);
            var jobs = page.Items.Select(j => new
            {
                id = j.Id,
                title = j.Title,
                company = j.Company,
                location = j.Location,
                salary = j.Salary,
                requiredSkills = j.RequiredSkills,
                isRemote = j.IsRemote,
                externalUrl = j.ExternalUrl
            });

            return JsonSerializer.Serialize(
                new { totalFound = page.TotalCount, jobs }, JsonOpts);
        }

        private async Task<string> ExecuteGetCareerRoadmapAsync(string argumentsJson)
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;
            var track = root.GetProperty("track").GetString()
                ?? throw new ArgumentException("track is required.");

            var matches = await _roadmapService.GetAllAsync(track);
            if (matches.Count == 0)
            {
                var all = await _roadmapService.GetAllAsync(null);
                var availableTracks = all.Select(r => r.Track).Distinct().ToList();
                return JsonSerializer.Serialize(new
                {
                    error = $"No roadmap found for track '{track}'.",
                    availableTracks
                }, JsonOpts);
            }

            var roadmap = matches[0];
            var steps = roadmap.Steps.OrderBy(s => s.OrderIndex).Select(s => new
            {
                title = s.Title,
                description = s.Description,
                level = s.Level,
                resources = s.Resources
            });

            return JsonSerializer.Serialize(new
            {
                track = roadmap.Track,
                title = roadmap.Title,
                description = roadmap.Description,
                steps
            }, JsonOpts);
        }

        private async Task<string> ExecuteAnalyzeCvAsync(string userId)
        {
            var feedback = await _cvFeedbackService.GetFeedbackAsync(userId);
            var topSuggestions = feedback.Suggestions
                .OrderBy(s => PriorityOrder(s.Priority))
                .Take(5)
                .Select(s => new
                {
                    category = s.Category,
                    issue = s.Issue,
                    recommendation = s.Recommendation,
                    priority = s.Priority
                });

            return JsonSerializer.Serialize(new
            {
                overallSummary = feedback.OverallSummary,
                overallScore = feedback.OverallScore,
                suggestions = topSuggestions,
                strengths = feedback.Strengths,
                missingKeywords = feedback.MissingKeywords
            }, JsonOpts);
        }

        private async Task<string> ExecuteGetUserProfileAsync(string userId)
        {
            var profile = await _userService.GetProfileAsync(userId);
            return JsonSerializer.Serialize(profile, JsonOpts);
        }

        private static int PriorityOrder(string priority) => priority switch
        {
            "High" => 0,
            "Medium" => 1,
            "Low" => 2,
            _ => 3
        };
    }
}
