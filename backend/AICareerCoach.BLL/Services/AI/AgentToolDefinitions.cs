using System.ClientModel;
using System.Collections.Generic;
using OpenAI.Chat;

namespace AICareerCoach.BLL.Services.AI
{
    public static class AgentToolDefinitions
    {
        public static readonly ChatTool SearchJobs = ChatTool.CreateFunctionTool(
            functionName: "search_jobs",
            functionDescription: "Search the live job board. Returns the top 3 matches plus `totalFound` (e.g., { totalFound: 27, jobs: [3 items] }). Use `query` for the search term and `location` only if the user specified one. Omit `location` for location-agnostic search.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "query": {
                      "type": "string",
                      "description": "Search query (job title, skill, or keyword). Example: '.NET developer', 'Angular'."
                    },
                    "location": {
                      "type": "string",
                      "description": "Optional location filter. Example: 'Cairo', 'Remote'. Omit if user did not mention a location."
                    }
                  },
                  "required": ["query"]
                }
                """));

        public static readonly ChatTool GetCareerRoadmap = ChatTool.CreateFunctionTool(
            functionName: "get_career_roadmap",
            functionDescription: "Get the curated learning roadmap for one of the 6 valid tracks. Track MUST be one of: Backend, Frontend, Full Stack, ML, DevOps, Data Analyst (case-sensitive). Returns the full ordered step list with levels + resources.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "track": {
                      "type": "string",
                      "enum": ["Backend", "Frontend", "Full Stack", "ML", "DevOps", "Data Analyst"],
                      "description": "One of the 6 valid track names (case-sensitive)."
                    }
                  },
                  "required": ["track"]
                }
                """));

        public static readonly ChatTool AnalyzeCv = ChatTool.CreateFunctionTool(
            functionName: "analyze_cv",
            functionDescription: "Analyze the user's latest uploaded CV. Returns `overallSummary`, `overallScore` (0-100), top 5 suggestions (priority-ordered: High → Medium → Low), `strengths`, and `missingKeywords`. If no CV is uploaded, returns { error: 'No CV found. Please upload your CV first.' } — tell the user to upload.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {},
                  "required": []
                }
                """));

        public static readonly ChatTool GetUserProfile = ChatTool.CreateFunctionTool(
            functionName: "get_user_profile",
            functionDescription: "Get the user's profile: full name, email, career goal, CV count (hasCV = cvCount > 0), and roles. Call this first if you're unsure whether the user has a CV or what their career goal is.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {},
                  "required": []
                }
                """));

        public static IReadOnlyList<ChatTool> AllTools { get; } = new[]
        {
            SearchJobs,
            GetCareerRoadmap,
            AnalyzeCv,
            GetUserProfile
        };
    }
}
