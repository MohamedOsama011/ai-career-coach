export interface RoadmapTemplateDto {
  id: number;
  track: string;
  title: string;
  description: string;
  orderIndex: number;
  steps: RoadmapStepDto[];
}

export interface RoadmapStepDto {
  id: number;
  roadmapId: number;
  title: string;
  description: string;
  level: string;
  resources: string[];
  orderIndex: number;
}

export interface SkillGapItemDto {
  skillName: string;
  currentLevel: string;
  requiredLevel: string;
  gap: string;
  priority: string;
}

export interface SkillsCategoryDto {
  category: string;
  skills: SkillGapItemDto[];
}

export interface RoadmapStepResultDto {
  order: number;
  title: string;
  description: string;
  level: string;
  resources: string[];
  duration: string | null;
}

export interface TemplateSnapshotDto {
  id: number;
  track: string;
  title: string;
  description: string;
  steps: RoadmapStepDto[];
}

export interface UserRoadmapDto {
  id: number;
  targetRole: string;
  templateTrack: string;
  steps: RoadmapStepResultDto[];
  gapAnalysis: SkillsCategoryDto[];
  createdAt: string;
  matchScore?: number;
  templateSnapshot?: TemplateSnapshotDto;
  currentSeniority?: string;
  targetSeniority?: string;
  seniorityGap?: string;
}

export interface GenerateRoadmapRequestDto {
  targetRole: string;
  templateTrack?: string;
  forceRegenerate?: boolean;
}
