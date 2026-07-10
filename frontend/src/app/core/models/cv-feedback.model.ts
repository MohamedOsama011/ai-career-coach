export interface FeedbackSuggestion {
  category: string;
  issue: string;
  recommendation: string;
  priority: 'High' | 'Medium' | 'Low';
  originalText?: string;
  suggestedText?: string;
}

export interface CvFeedback {
  overallScore: number;
  keywordMatch: number;
  impactStatements: number;
  formatting: number;
  leadershipSignals: number;
  overallSummary: string;
  strengths: string[];
  missingKeywords: string[];
  suggestions: FeedbackSuggestion[];
  fromCache: boolean;
  generatedAt: string;
}