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
    get_recommended_jobs: 'Recommended jobs',
    get_personal_roadmap: 'Personal roadmap',
  analyze_cv: 'Analyzed CV',
  get_user_profile: 'Checked profile'
};
