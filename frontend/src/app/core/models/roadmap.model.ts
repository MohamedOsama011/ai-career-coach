export interface Roadmap {
  id: number;
  track: string;
  title: string;
  description: string;
  orderIndex: number;
  steps: RoadmapStep[];
}

export interface RoadmapStep {
  id: number;
  roadmapId: number;
  title: string;
  description: string;
  level: string;
  resources: string[];
  orderIndex: number;
  week: string;
  status: 'complete' | 'in_progress' | 'upcoming';
}
