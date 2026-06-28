import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CareerProfileStore } from '../../core/store/career-profile-store';
import { SkillsCategoryDto } from '../../core/models/roadmap.model';

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

function levelToPercent(level: string): number {
  switch (level) {
    case 'Expert':       return 100;
    case 'Advanced':     return 75;
    case 'Intermediate': return 50;
    case 'Beginner':     return 25;
    case 'None':         return 0;
    default:             return 0;
  }
}

const TRACK_LABELS: Record<string, string> = {
  Behavioral: 'Behavioral',
  TechnicalCoding: 'Technical Coding',
  SystemDesign: 'System Design'
};

function trackLabel(value: string): string {
  return TRACK_LABELS[value] ?? value;
}

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  userName = signal('');
  greeting = signal('GOOD MORNING');
  roadmapSegments = Array(12).fill(0);

  private authService = inject(AuthService);
  private store = inject(CareerProfileStore);
  private router = inject(Router);

  loading = this.store.loading;
  error = this.store.error;

  cvScore = computed(() => this.store.cvScore());
  cvTopMatches = computed(() => {
    const titles = this.store.topRecommendations().slice(0, 2).map(r => r.title);
    return titles.length > 0 ? titles.join(', ') : 'Upload a CV to see matches';
  });
  roadmapTotal = computed(() => this.store.userRoadmap()?.steps.length ?? 0);
  roadmapNext = computed(() => this.store.userRoadmap()?.steps[0]?.title ?? 'Generate a roadmap to see next steps');
  interviewGrade = computed(() => this.store.interviewGrade() ?? '—');
  interviewSessions = computed(() => this.store.interviewSessionsCount());
  interviewTopics = computed(() =>
    Array.from(new Set(this.store.interviewHistory().map(h => h.track)))
  );

  trackLabel = trackLabel;

  skills = computed<DashboardSkill[]>(() => {
    const flat = this.store.skillsGap().flatMap(cat => cat.skills);
    const sorted = CareerProfileStore.sortCategories(
      [{ category: '', skills: flat } as SkillsCategoryDto],
      CareerProfileStore.readSortMode()
    )[0].skills;
    return sorted.map(s => {
      const percent = levelToPercent(s.currentLevel);
      return {
        name: s.skillName,
        progress: percent,
        status: percent === 100 ? 'MASTERED' : 'GAP FOUND',
        type: percent === 100 ? 'mastered'
            : percent >= 65  ? 'strong'
            : percent >= 30  ? 'leveling'
            : 'gap'
      } as DashboardSkill;
    });
  });

  events: DashboardEvent[] = [];

  recommendations = computed<DashboardRecommendation[]>(() =>
    this.store.topRecommendations().slice(0, 3).map(r => ({
      match: r.matchScore,
      title: r.title,
      company: r.company,
      salary: r.salary > 0 ? `$${Math.round(r.salary / 1000)}k+` : 'Competitive'
    }))
  );

  ngOnInit(): void {
    const hour = new Date().getHours();
    if (hour < 12) this.greeting.set('GOOD MORNING');
    else if (hour < 18) this.greeting.set('GOOD AFTERNOON');
    else this.greeting.set('GOOD EVENING');
    this.userName.set(this.authService.getUserFullName());
    this.store.refreshAll();
  }

  startInterview(): void {
    this.router.navigate(['/interview']);
  }

  viewJobs(): void {
    this.router.navigate(['/jobs']);
  }

  viewRoadmap(): void {
    this.router.navigate(['/roadmap']);
  }

  viewSkills(): void {
    this.router.navigate(['/skills']);
  }

  viewCV(): void {
    this.router.navigate(['/cv']);
  }
}
