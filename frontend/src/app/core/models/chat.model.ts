export interface SendChatMessageRequest {
  message: string;
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  toolsUsed?: string[];
}

export interface ChatSession {
  id: number;
  title: string | null;
  createdAt: string;
  updatedAt: string;
  messages: ChatMessage[];
}

export interface ChatSessionSummary {
  id: number;
  title: string | null;
  createdAt: string;
  updatedAt: string;
}

export const TOOL_LABELS: Record<string, string> = {
  search_jobs: 'Searched jobs',
  get_career_roadmap: 'Fetched roadmap',
  analyze_cv: 'Analyzed CV',
  get_user_profile: 'Checked profile'
};
