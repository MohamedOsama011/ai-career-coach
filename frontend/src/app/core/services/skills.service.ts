import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

export interface SkillItem {
  name: string;
  current: number;
  target: number;
}

export interface SkillCategory {
  title: string;
  skills: SkillItem[];
}

@Injectable({
  providedIn: 'root'
})
export class SkillsService {
  private mockCategories: SkillCategory[] = [
    {
      title: 'Core Engineering',
      skills: [
        { name: 'TypeScript', current: 95, target: 95 },
        { name: 'React', current: 88, target: 90 },
        { name: 'Testing', current: 72, target: 85 }
      ]
    },
    {
      title: 'Backend & Infra',
      skills: [
        { name: 'Node.js', current: 40, target: 80 },
        { name: 'PostgreSQL', current: 35, target: 70 },
        { name: 'Caching', current: 30, target: 70 }
      ]
    },
    {
      title: 'Architecture & Leadership',
      skills: [
        { name: 'System Design', current: 65, target: 85 },
        { name: 'Mentorship', current: 55, target: 80 },
        { name: 'Stakeholder Mgmt', current: 45, target: 75 }
      ]
    }
  ];

  getSkillsAnalysis(): Observable<SkillCategory[]> {
    return of(this.mockCategories);
  }
}
