using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Helpers;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AICareerCoach.BLL.Services.AI
{
    public class RoadmapLlmService : IRoadmapLlmService
    {
        private const int CvTrimLimit = CvConstants.MaxLength;
        private const int CvExcerptLimit = CvConstants.MaxLength;

        private readonly ChatClient _chatClient;
        private readonly ILogger<RoadmapLlmService> _logger;

        public RoadmapLlmService(IConfiguration config, ILogger<RoadmapLlmService> logger)
        {
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
        }

        /// <summary>
        /// Two-pass gap-driven roadmap generation.
        /// Pass 1: assess seniority + identify gaps.
        /// Pass 2: build steps for the gaps only, ordered by priority.
        /// The reference <paramref name="template"/> is used for tech-stack context
        /// only — it is NOT a rigid step skeleton.
        /// </summary>
        public async Task<(List<RoadmapStepResultDto> Steps, List<SkillsCategoryDto> GapAnalysis, CandidateAssessmentDto? Assessment)> GenerateRoadmapAsync(
            string cvText, string targetRole, Roadmap template)
        {
            // ── Pass 1: Assess ──
            var (assessment, gaps) = await AssessCandidateAsync(cvText, targetRole, template);
            if (assessment is null || gaps is null)
            {
                _logger.LogWarning("Pass 1 (assessment) failed, activating fallback.");
                var fallback = GetFallback(template);
                return (fallback.Steps, fallback.GapAnalysis, null);
            }

            // ── Pass 2: Plan ──
            var steps = await GenerateGapBasedStepsAsync(assessment, gaps, targetRole, template, cvText);
            if (steps is null || steps.Count == 0)
            {
                _logger.LogWarning("Pass 2 (step generation) failed, using template steps with real gap analysis.");
                var fallback = GetFallback(template);
                return (fallback.Steps, gaps, assessment);
            }

            return (steps, gaps, assessment);
        }

        /// <summary>
        /// Re-assess gaps + seniority for an existing roadmap (rescan).
        /// </summary>
        public async Task<(List<SkillsCategoryDto> GapAnalysis, CandidateAssessmentDto? Assessment)> GenerateGapAnalysisAsync(
            string cvText, string targetRole, Roadmap template)
        {
            var (assessment, gaps) = await AssessCandidateAsync(cvText, targetRole, template);
            if (assessment is null || gaps is null)
            {
                _logger.LogWarning("Rescan assessment failed, activating fallback.");
                return (GetFallback(template).GapAnalysis, null);
            }

            return (gaps, assessment);
        }

        public async Task<List<RoadmapStepResultDto>> GenerateWeaknessStepsAsync(
            List<string> weakAreas, string cvText, string targetRole)
        {
            if (weakAreas == null || weakAreas.Count == 0)
                return new List<RoadmapStepResultDto>();

            var trimmedCv = cvText.Length > CvConstants.MaxLength ? cvText[..CvConstants.MaxLength] : cvText;

            var jsonStructure = """
            {
              "steps": [
                {
                  "order": 1,
                  "title": "Short actionable title (max 8 words)",
                  "description": "1-2 sentence explanation of what to do and why",
                  "level": "Beginner or Intermediate or Advanced",
                  "resources": ["https://example.com/relevant-resource"],
                  "duration": "1 week or 2 weeks etc."
                }
              ]
            }
            """;

            var weaknessList = string.Join("\n", weakAreas.Select((w, i) => $"{i + 1}. {w}"));

            var systemPrompt = $"""
                You are an expert technical mentor. For each weak area identified in a candidate's mock interview, produce ONE concrete, actionable roadmap step that the candidate can take to improve.

                You must return ONLY a valid JSON object matching this exact structure (no markdown, no ```json tags, no backticks):

                {jsonStructure}

                The 'steps' array must contain EXACTLY {weakAreas.Count} item(s) — one per weak area, in the same order.

                All text must be in English.
                """;

            var userPrompt = $"""
                TARGET ROLE: {targetRole}

                WEAK AREAS (one step per area):
                {weaknessList}

                CANDIDATE CV (for context, to tailor the advice):
                {trimmedCv}
                """;

            try
            {
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await _chatClient.CompleteChatAsync(
                    messages, new ChatCompletionOptions { Temperature = 0.3f }, cts.Token);

                var rawJson = response.Value.Content[0].Text.Trim()
                    .Replace("```json", "").Replace("```", "");

                var parsed = JsonSerializer.Deserialize<RoadmapLlmResponse>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var steps = parsed?.Steps ?? new List<RoadmapStepResultDto>();

                if (steps.Count == 0)
                    return BuildFallbackSteps(weakAreas);

                if (steps.Count > weakAreas.Count)
                    steps = steps.Take(weakAreas.Count).ToList();

                for (int i = 0; i < steps.Count; i++)
                {
                    steps[i].Order = i + 1;
                }

                return steps;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate weakness steps via AI, activating fallback.");
                return BuildFallbackSteps(weakAreas);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  PASS 1 — Assess candidate seniority + identify skill gaps
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Assesses the candidate's current seniority, the target role's required
        /// seniority, and the specific skill gaps between them. Returns the
        /// seniority-only DTO (for persistence) alongside the full gap analysis
        /// (for Pass 2 consumption).
        /// </summary>
        private async Task<(CandidateAssessmentDto? Assessment, List<SkillsCategoryDto>? Gaps)> AssessCandidateAsync(
            string cvText, string targetRole, Roadmap template)
        {
            var trimmedCv = cvText.Length > CvTrimLimit ? cvText[..CvTrimLimit] : cvText;

            var templateStepsText = string.Join("\n", template.Steps.OrderBy(s => s.OrderIndex).Select(s =>
                $"  Step {s.OrderIndex} [{s.Level}]: {s.Title} - {s.Description}"));

            var jsonStructure = """
            {
              "currentSeniority": "Junior or Mid or Senior",
              "targetSeniority": "Junior or Mid or Senior",
              "seniorityGap": "Short description of the seniority jump and what to focus on",
              "gapAnalysis": [
                {
                  "category": "Technical Skills or Soft Skills or Tools & Technologies",
                  "skills": [
                    {
                      "skillName": "Skill name",
                      "currentLevel": "None or Beginner or Intermediate or Advanced",
                      "requiredLevel": "Beginner or Intermediate or Advanced",
                      "gap": "Explain what the candidate is missing",
                      "priority": "High or Medium or Low"
                    }
                  ]
                }
              ]
            }
            """;

            var systemPrompt = $"""
                You are an expert technical mentor and career coach. Your task is to assess a candidate's current skill level against a target role and identify the SPECIFIC GAPS they need to close to reach that role.

                [INPUT DATA]
                TARGET ROLE: {targetRole}
                REFERENCE TEMPLATE TRACK: {template.Track}
                REFERENCE TEMPLATE TITLE: {template.Title}
                REFERENCE TEMPLATE STEPS (tech-stack context only, NOT a skeleton to follow):
                {templateStepsText}

                [CRITICAL INSTRUCTIONS]
                1. SENIORITY ASSESSMENT:
                   - currentSeniority: Infer the candidate's current seniority from CV evidence (years of experience, project complexity, tech depth, leadership signals). Use "Junior" if entry-level, "Mid" if 2-4 years with solid fundamentals, "Senior" if 5+ years with architecture/leadership evidence. If unclear, default to "Mid".
                   - targetSeniority: Infer from the target role string. "Senior .NET Developer" → "Senior", "Junior Frontend" → "Junior", ".NET Developer" (no qualifier) → "Mid".
                   - seniorityGap: A short human-readable description of the jump (e.g. "Mid to Senior — focus on system design, architecture, and mentoring" or "Junior to Mid — solidify fundamentals and gain production experience").

                2. GAP IDENTIFICATION:
                   - Identify ONLY the skills the candidate is MISSING or WEAK in relative to the TARGET ROLE.
                   - Do NOT include skills the candidate has already mastered at or above the required level.
                   - If the candidate is Mid-level or above targeting Senior, focus gaps on: system design, architecture, advanced patterns, performance, scalability, mentoring, leadership — NOT basics.
                   - If the candidate is Junior targeting Mid, focus gaps on: production fundamentals, testing, debugging, framework depth.
                   - Produce 3-10 gaps categorized into 2-3 categories (e.g. Technical Skills, Tools & Technologies, Soft Skills).
                   - Each gap: skillName, currentLevel (None/Beginner/Intermediate/Advanced — from CV evidence, "None" if completely missing), requiredLevel (what the TARGET ROLE demands), gap (explanation), priority (High/Medium/Low — High = critical for the target role).

                3. TECH-STACK PRECEDENCE: The TARGET ROLE takes absolute precedence over the REFERENCE TEMPLATE tech-stack. If the target role specifies a different stack (e.g. MERN vs .NET), assess gaps against the target stack, not the template stack.

                You must return ONLY a valid JSON object matching this exact structure (no markdown, no ```json tags, no backticks):
                {jsonStructure}

                All text must be in English.
                """;

            try
            {
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage($"CANDIDATE CV:\n{trimmedCv}")
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await _chatClient.CompleteChatAsync(messages, new ChatCompletionOptions { Temperature = 0.3f }, cts.Token);

                var rawJson = response.Value.Content[0].Text.Trim()
                    .Replace("```json", "").Replace("```", "");

                var parsed = JsonSerializer.Deserialize<AssessmentResponse>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed is null || parsed.GapAnalysis is null)
                    return (null, null);

                var assessment = new CandidateAssessmentDto
                {
                    CurrentSeniority = parsed.CurrentSeniority ?? "Mid",
                    TargetSeniority = parsed.TargetSeniority ?? "Mid",
                    SeniorityGap = parsed.SeniorityGap ?? string.Empty,
                };

                return (assessment, parsed.GapAnalysis);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pass 1 (assessment) LLM call failed.");
                return (null, null);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  PASS 2 — Build gap-driven steps (priority-ordered, no fundamentals)
        // ════════════════════════════════════════════════════════════════

        private async Task<List<RoadmapStepResultDto>?> GenerateGapBasedStepsAsync(
            CandidateAssessmentDto assessment, List<SkillsCategoryDto> gaps, string targetRole, Roadmap template, string cvText)
        {
            var trimmedCv = cvText.Length > CvExcerptLimit ? cvText[..CvExcerptLimit] : cvText;

            var gapsText = FlattenGaps(gaps);

            var jsonStructure = """
            {
              "steps": [
                {
                  "order": 1,
                  "title": "Actionable step title",
                  "description": "1-2 sentence explanation of what to do and why",
                  "level": "Beginner or Intermediate or Advanced",
                  "resources": ["https://...", "https://..."],
                  "duration": "2 weeks or 1 month etc."
                }
              ]
            }
            """;

            var systemPrompt = $"""
                You are an expert technical mentor. Given a candidate assessment (seniority + identified skill gaps), produce a personalized learning roadmap with concrete, actionable steps.

                [INPUT DATA]
                TARGET ROLE: {targetRole}
                REFERENCE TEMPLATE TRACK: {template.Track} (tech-stack context only, NOT a skeleton)

                CANDIDATE ASSESSMENT:
                - Current Seniority: {assessment.CurrentSeniority}
                - Target Seniority: {assessment.TargetSeniority}
                - Seniority Gap: {assessment.SeniorityGap}

                IDENTIFIED GAPS (build steps for THESE ONLY):
                {gapsText}

                [CRITICAL INSTRUCTIONS]
                1. GAP-DRIVEN STEPS: Build steps ONLY for the identified gaps above. Each step must address one or more gaps. Do NOT add steps for skills the candidate already has.
                2. PRIORITY ORDER: Order steps by gap priority (High first, then Medium, then Low) — NOT by difficulty. The first step should tackle the most critical gap.
                3. SKIP FUNDAMENTALS: If the candidate is Mid-level or above, do NOT include introductory/fundamental steps (e.g. "C# Fundamentals" for a Mid .NET developer). Start at the appropriate level for their seniority.
                4. VARIABLE STEP COUNT: Produce 3-8 steps depending on the number of gaps. Fewer gaps = fewer steps. Do NOT pad to reach a fixed count.
                5. SENIORITY-AWARE CONTENT:
                   - Mid → Senior: focus on system design, architecture, advanced patterns, performance, scalability, mentoring, technical leadership.
                   - Junior → Mid: focus on production fundamentals, framework depth, testing, debugging, best practices.
                   - Same level: focus on breadth/polish across the target stack.
                6. ACTIONABLE: Each step must have a clear title, 1-2 sentence description, level (Beginner/Intermediate/Advanced), 1-3 resource URLs, and estimated duration.
                7. TECH-STACK: Use the TARGET ROLE's tech-stack, not the template's (unless they match).
                8. If no gaps are identified, produce 1-2 advanced enrichment/polish steps appropriate for the target seniority.

                You must return ONLY a valid JSON object matching this exact structure (no markdown, no ```json tags, no backticks):
                {jsonStructure}

                All text must be in English.
                """;

            var userPrompt = $"""
                CANDIDATE CV (for context, to tailor the steps):
                {trimmedCv}
                """;

            try
            {
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await _chatClient.CompleteChatAsync(messages, new ChatCompletionOptions { Temperature = 0.3f }, cts.Token);

                var rawJson = response.Value.Content[0].Text.Trim()
                    .Replace("```json", "").Replace("```", "");

                var parsed = JsonSerializer.Deserialize<RoadmapLlmResponse>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var steps = parsed?.Steps;
                if (steps is null || steps.Count == 0)
                    return null;

                // Ensure sequential ordering
                for (int i = 0; i < steps.Count; i++)
                    steps[i].Order = i + 1;

                return steps;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pass 2 (step generation) LLM call failed.");
                return null;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Helpers
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Flattens the gap analysis into a numbered, priority-sorted text list
        /// for the Pass 2 prompt.
        /// </summary>
        private static string FlattenGaps(List<SkillsCategoryDto> gapAnalysis)
        {
            var allGaps = new List<(string SkillName, string CurrentLevel, string RequiredLevel, string Gap, string Priority)>();

            foreach (var category in gapAnalysis)
            {
                foreach (var skill in category.Skills)
                {
                    allGaps.Add((skill.SkillName, skill.CurrentLevel, skill.RequiredLevel, skill.Gap, skill.Priority));
                }
            }

            var priorityOrder = new Dictionary<string, int> { ["High"] = 0, ["Medium"] = 1, ["Low"] = 2 };
            allGaps = allGaps
                .OrderBy(g => priorityOrder.TryGetValue(g.Priority, out var p) ? p : 3)
                .ThenBy(g => g.SkillName)
                .ToList();

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < allGaps.Count; i++)
            {
                var g = allGaps[i];
                sb.AppendLine($"  {i + 1}. [{g.Priority}] {g.SkillName} — current: {g.CurrentLevel}, required: {g.RequiredLevel} — gap: {g.Gap}");
            }

            return sb.ToString();
        }

        private static List<RoadmapStepResultDto> BuildFallbackSteps(List<string> weakAreas)
        {
            return weakAreas.Select((area, i) => new RoadmapStepResultDto
            {
                Order = i + 1,
                Title = Truncate(area, 60),
                Description = $"Work on improving: {area}. Review relevant learning resources and practice the concept.",
                Level = "Intermediate",
                Resources = new List<string>(),
                Duration = "2 weeks"
            }).ToList();
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s[..max].TrimEnd() + "…";
        }

        private static (List<RoadmapStepResultDto> Steps, List<SkillsCategoryDto> GapAnalysis) GetFallback(Roadmap template)
        {
            var steps = template.Steps.OrderBy(s => s.OrderIndex).Select(s => new RoadmapStepResultDto
            {
                Order = s.OrderIndex,
                Title = s.Title,
                Description = s.Description,
                Level = s.Level,
                Resources = JsonSerializer.Deserialize<List<string>>(s.Resources) ?? new(),
                Duration = null
            }).ToList();

            var gapAnalysis = new List<SkillsCategoryDto>
            {
                new()
                {
                    Category = "Technical Skills",
                    Skills = new List<SkillGapItemDto>
                    {
                        new() { SkillName = template.Title, CurrentLevel = "Beginner", RequiredLevel = "Advanced", Gap = "Review the roadmap steps above and assess your current knowledge.", Priority = "High" }
                    }
                }
            };

            return (steps, gapAnalysis);
        }

        internal class RoadmapLlmResponse
        {
            public List<RoadmapStepResultDto>? Steps { get; set; }
            public List<SkillsCategoryDto>? GapAnalysis { get; set; }
        }

        internal class AssessmentResponse
        {
            public string? CurrentSeniority { get; set; }
            public string? TargetSeniority { get; set; }
            public string? SeniorityGap { get; set; }
            public List<SkillsCategoryDto>? GapAnalysis { get; set; }
        }
    }
}
