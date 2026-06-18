using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.CV
{
    public class CvFeedbackDto
    {
        public string OverallSummary { get; set; } = string.Empty;
        public int OverallScore { get; set; }          
        public List<FeedbackSuggestion> Suggestions { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
        public List<string> MissingKeywords { get; set; } = new();
        public bool FromCache { get; set; }
        public DateTime GeneratedAt { get; set; }

        public int KeywordMatch { get; set; }       
        public int ImpactStatements { get; set; }   
        public int Formatting { get; set; }         
        public int LeadershipSignals { get; set; }
    }
    public class FeedbackSuggestion
    {
        public string Category { get; set; } = string.Empty;  
        public string Issue { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;  
    }
}
