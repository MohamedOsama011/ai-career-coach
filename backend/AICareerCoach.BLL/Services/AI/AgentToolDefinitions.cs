using System.ClientModel;
using System.Collections.Generic;
using OpenAI.Chat;

namespace AICareerCoach.BLL.Services.AI
{
    public static class AgentToolDefinitions
    {
        public static readonly ChatTool GetRecommendedJobs = ChatTool.CreateFunctionTool(
            functionName: "get_recommended_jobs",
            functionDescription: "Get personalized job recommendations based on your CV analysis. Uses AI-powered matching (cosine similarity on CV embeddings) to find jobs that fit your profile. Returns top matches with match scores, explanations, and missing skills you need to develop.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {},
                  "required": []
                }
                """));

        public static readonly ChatTool GetPersonalRoadmap = ChatTool.CreateFunctionTool(
            functionName: "get_personal_roadmap",
            functionDescription: "Get your personalized learning roadmap. Returns a gap-driven roadmap with priority-ordered steps based on your CV analysis, current/target seniority levels, and skills gap analysis. Generate one from the Roadmap page first if none exists.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {},
                  "required": []
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
            GetRecommendedJobs,
            GetPersonalRoadmap,
            AnalyzeCv,
            GetUserProfile
        };
    }
}
