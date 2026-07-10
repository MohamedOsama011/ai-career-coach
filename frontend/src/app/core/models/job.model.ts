export interface Job {
  id: number;
  title: string;
  company: string;
  description?: string;
  location: string;
  requiredSkills: string[];
  salary: string;
  postedAt: string;
  matchPercentage: number;
  logoInitials: string;
  saved?: boolean;
  companyLogoUrl?: string;
  externalUrl?: string;
  contractType?: string;
  isRemote?: boolean;
  category?: string;
  source?: string;
}

export interface JobRecommendation {
  jobId: number;
  title: string;
  company: string;
  description: string;
  companyLogoUrl?: string;
  salary: number;
  location: string;
  externalUrl?: string;
  matchScore: number;
  matchExplanation: string;
  missingSkills?: string[];
}

export interface JobRecommendationResult {
  userId: string;
  recommendations: JobRecommendation[];
  generatedAt: string;
  isLimited?: boolean;
  totalCount?: number;
  returnedCount?: number;
}

export interface SyncResultDto {
  fetched: number;
  new: number;
  skipped: number;
  embedded: number;
  errors: number;
  syncedAt: string;
  errorMessages: string[];
}

export interface SyncStatusDto {
  lastSyncAt?: string;
  lastSyncNew?: number;
  lastSyncErrors?: number;
  enabled: boolean;
  intervalHours: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrev: boolean;
}

export interface UpdateJobDto {
  title: string;
  company: string;
  description: string;
  requiredSkills: string[];
  location: string;
  salary: number;
  companyLogoUrl?: string;
  isRemote?: boolean;
  externalUrl?: string;
}
