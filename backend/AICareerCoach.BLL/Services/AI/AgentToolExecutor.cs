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
        private readonly IJobRecommendationService _jobRecommendationService;
        private readonly IUserRoadmapService _userRoadmapService;
        private readonly ICvFeedbackService _cvFeedbackService;
        private readonly IUserService _userService;
        private readonly ILogger<AgentToolExecutor> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AgentToolExecutor(
            IJobRecommendationService jobRecommendationService,
            IUserRoadmapService userRoadmapService,
            ICvFeedbackService cvFeedbackService,
            IUserService userService,
            ILogger<AgentToolExecutor> logger)
        {
            _jobRecommendationService = jobRecommendationService;
            _userRoadmapService = userRoadmapService;
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
                    "get_recommended_jobs" => await ExecuteGetRecommendedJobsAsync(userId),
                    "get_personal_roadmap" => await ExecuteGetPersonalRoadmapAsync(userId),
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

        private async Task<string> ExecuteGetRecommendedJobsAsync(string userId)
        {
            var result = await _jobRecommendationService.GetRecommendationsAsync(userId);

            var jobs = result.Recommendations.Select(j => new
            {
                title = j.Title,
                company = j.Company,
                salary = j.Salary,
                location = j.Location,
                matchScore = j.MatchScore,
                matchExplanation = j.MatchExplanation,
                missingSkills = j.MissingSkills,
                isRemote = string.IsNullOrEmpty(j.Location) ? null : (bool?)null,
                externalUrl = j.ExternalUrl
            });

            return JsonSerializer.Serialize(new
            {
                jobs,
                totalFound = result.TotalCount,
                returnedCount = result.ReturnedCount,
                isLimited = result.IsLimited
            }, JsonOpts);
        }

        private async Task<string> ExecuteGetPersonalRoadmapAsync(string userId)
        {
            var roadmap = await _userRoadmapService.GetMyRoadmapAsync(userId);
            if (roadmap is null)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "No personalized roadmap yet. Go to the Roadmap page and generate one with your target role first."
                }, JsonOpts);
            }

            var steps = roadmap.Steps.Select(s => new
            {
                title = s.Title,
                description = s.Description,
                level = s.Level,
                duration = s.Duration
            });

            var gapAnalysis = roadmap.GapAnalysis.Select(c => new
            {
                category = c.Category,
                skills = c.Skills.Select(s => new
                {
                    skillName = s.SkillName,
                    currentLevel = s.CurrentLevel,
                    requiredLevel = s.RequiredLevel,
                    gap = s.Gap,
                    priority = s.Priority
                })
            });

            return JsonSerializer.Serialize(new
            {
                targetRole = roadmap.TargetRole,
                currentSeniority = roadmap.CurrentSeniority,
                targetSeniority = roadmap.TargetSeniority,
                seniorityGap = roadmap.SeniorityGap,
                matchScore = roadmap.MatchScore,
                steps,
                gapAnalysis
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
