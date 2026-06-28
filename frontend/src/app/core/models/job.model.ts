export interface Job {
  id: number;
  title: string;
  company: string;
  location: string;
  requiredSkills: string[];
  salary: string;
  postedAt: string;
  matchPercentage: number;
  logoInitials: string;
  saved?: boolean;
  companyLogoUrl?: string;
}

export interface JobRecommendation {
  jobId: number;
  title: string;
  company: string;
  description: string;
  companyLogoUrl?: string;
  salary: number;
  location: string;
  matchScore: number;
  matchExplanation: string;
  missingSkills?: string[];
}

export interface JobRecommendationResult {
  userId: string;
  recommendations: JobRecommendation[];
  generatedAt: string;
}
