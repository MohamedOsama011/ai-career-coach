using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AICareerCoach.DAL.Entities
{
    /// <summary>Interview track selected by the candidate at setup.</summary>
    public enum InterviewTrack
    {
        [Display(Name = "Behavioral")]
        Behavioral,

        [Display(Name = "Technical Coding")]
        TechnicalCoding,

        [Display(Name = "System Design")]
        SystemDesign
    }

    /// <summary>Seniority calibration for question difficulty.</summary>
    public enum InterviewDifficulty
    {
        [Display(Name = "Junior")]
        Junior,

        [Display(Name = "Mid-Level")]
        MidLevel,

        [Display(Name = "Senior")]
        Senior
    }

    /// <summary>Lifecycle state of an interview session.</summary>
    public enum InterviewStatus
    {
        Active,
        Completed,
        /// <summary>Reserved for a future background reaper that marks stale
        /// Active sessions abandoned. Not assigned by any current code path.</summary>
        Abandoned
    }

    /// <summary>Author of a message in the interview transcript.</summary>
    public enum MessageRole
    {
        Interviewer,
        Candidate
    }

    /// <summary>
    /// Resolves the human-readable display name for an enum value via its
    /// <see cref="DisplayAttribute"/>, falling back to the raw enum name when
    /// no attribute is present. Single source of truth so the frontend never
    /// sees raw C# identifiers like "TechnicalCoding" or "MidLevel".
    /// </summary>
    public static class EnumDisplay
    {
        public static string Name<T>(T value) where T : struct, Enum
        {
            var member = typeof(T).GetMember(value.ToString());
            if (member.Length > 0)
            {
                var attr = member[0].GetCustomAttribute<DisplayAttribute>();
                if (attr?.Name is { Length: > 0 } name)
                    return name;
            }
            return value.ToString();
        }
    }
}
