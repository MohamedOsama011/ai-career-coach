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

        /// <summary>
        /// Streaming variant of <see cref="GenerateNextQuestionAsync"/>.
        /// Yields SSE events: token / done / error (Phase E, E.2).
        /// Yields a final token event containing the fallback question
        /// followed by an `error: fallback` event when LLM retries are
        /// exhausted (per locked decision L2: "stream the fallback, then
        /// send error event").
        /// </summary>
        IAsyncEnumerable<StreamTokenDto> GenerateNextQuestionStreamAsync(
            InterviewTrack track,
            InterviewDifficulty difficulty,
            string targetRole,
            string summaryContextJson,
            List<InterviewMessage> transcript,
            int nextTurnNumber,
            CancellationToken cancellationToken = default);

        Task<InterviewScorecardDto> GenerateScorecardAsync(
            List<InterviewMessage> transcript,
            InterviewTrack track,
            InterviewDifficulty difficulty,
            string targetRole,
            string cvExcerpt);
    }
}
