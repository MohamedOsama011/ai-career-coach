using AICareerCoach.BLL.DTOs.Interview;
using AICareerCoach.DAL.Entities;

namespace AICareerCoach.BLL.Interfaces.AI
{
    public interface IInterviewLlmService
    {
        Task<QuestionResult> GenerateNextQuestionAsync(
            InterviewTrack track,
            InterviewDifficulty difficulty,
            string targetRole,
            string summaryContextJson,
            List<InterviewMessage> transcript,
            int nextTurnNumber);

        Task<InterviewScorecardDto> GenerateScorecardAsync(
            List<InterviewMessage> transcript,
            InterviewTrack track,
            InterviewDifficulty difficulty,
            string targetRole,
            string cvExcerpt);
    }
}
