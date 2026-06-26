export interface InterviewOptionItem {
  value: string;
  label: string;
}

export interface InterviewOptionsDto {
  tracks: InterviewOptionItem[];
  difficulties: InterviewOptionItem[];
}

export interface StartSessionRequestDto {
  track: string;
  difficulty: string;
  targetRole: string;
}

export interface SubmitAnswerRequestDto {
  answer: string;
}

export interface InterviewMessageDto {
  id: number;
  role: string;
  turnNumber: number;
  content: string;
  createdAt: string;
}

export interface InterviewSessionDto {
  id: number;
  track: string;
  difficulty: string;
  targetRole: string;
  status: string;
  questionsAsked: number;
  maxQuestions: number;
  messages: InterviewMessageDto[];
  createdAt: string;
  completedAt: string | null;
}

export interface InterviewHistoryItemDto {
  id: number;
  track: string;
  difficulty: string;
  targetRole: string;
  status: string;
  questionsAsked: number;
  overallScore: number | null;
  letterGrade: string | null;
  overallSummary: string | null;
  createdAt: string;
  completedAt: string | null;
}

export interface QuestionAnalysisItemDto {
  question: string;
  answer: string;
  rating: string;
  explanation: string;
  improvementSuggestion: string;
}

export interface InterviewScorecardDto {
  overallScore: number;
  letterGrade: string;
  overallSummary: string;
  strengths: string[];
  areasForImprovement: string[];
  questionAnalysis: QuestionAnalysisItemDto[];
}
