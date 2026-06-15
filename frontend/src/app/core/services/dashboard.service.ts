import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

export interface DashboardMetrics {
  cvScore: number;
  cvScoreChange: string;
  cvTopMatches: string;
  roadmapCompleted: number;
  roadmapTotal: number;
  roadmapNext: string;
  interviewGrade: string;
  interviewSessions: number;
  interviewTopics: string[];
}

export interface DashboardSkill {
  name: string;
  progress: number;
  status: string;
  type: 'mastered' | 'strong' | 'leveling' | 'gap';
}

export interface DashboardEvent {
  day: string;
  title: string;
}

export interface DashboardRecommendation {
  match: number;
  title: string;
  company: string;
  salary: string;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private metrics: DashboardMetrics = {
    cvScore: 84,
    cvScoreChange: '+12% wk',
    cvTopMatches: 'Senior Frontend, UI Engineer',
    roadmapCompleted: 7,
    roadmapTotal: 12,
    roadmapNext: 'Master Next.js App Router',
    interviewGrade: 'B+',
    interviewSessions: 3,
    interviewTopics: ['Behavioral', 'Technical', 'System Design']
  };

  private skills: DashboardSkill[] = [
    { name: 'TypeScript', progress: 100, status: 'MASTERED', type: 'mastered' },
    { name: 'React', progress: 80, status: 'STRONG', type: 'strong' },
    { name: 'System Design', progress: 60, status: 'LEVELING', type: 'leveling' },
    { name: 'Node.js', progress: 40, status: 'GAP FOUND', type: 'gap' },
    { name: 'GraphQL', progress: 25, status: 'GAP FOUND', type: 'gap' }
  ];

  private events: DashboardEvent[] = [
    { day: 'TUE', title: 'Behavioral mock at 10:00' },
    { day: 'WED', title: 'Roadmap check-in' },
    { day: 'FRI', title: 'Apply: Vercel Senior Eng' }
  ];

  private recommendations: DashboardRecommendation[] = [
    { match: 98, title: 'Senior Engineer', company: 'Vercel • Remote', salary: '$210k+' },
    { match: 92, title: 'Staff Frontend', company: 'Linear • SF/Hybrid', salary: '$240k+' },
    { match: 89, title: 'Product Lead', company: 'Stripe • NYC', salary: '$200k+' }
  ];

  getMetrics(): Observable<DashboardMetrics> {
    return of(this.metrics);
  }

  getSkillsGap(): Observable<DashboardSkill[]> {
    return of(this.skills);
  }

  getUpcomingEvents(): Observable<DashboardEvent[]> {
    return of(this.events);
  }

  getRecommendations(): Observable<DashboardRecommendation[]> {
    return of(this.recommendations);
  }
}
